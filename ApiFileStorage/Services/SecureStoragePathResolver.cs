using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Win32.SafeHandles;
using ApiFileStorage.Configuration;

namespace ApiFileStorage.Services;

public sealed record AllowedStorageRoot(
    string Id,
    string ConfiguredPath,
    string CanonicalPath);

public enum StorageObjectKind
{
    File,
    Directory
}

public sealed record StorageObjectMetadata(
    string RootId,
    string RequestedRelativePath,
    string CanonicalRelativePath,
    StorageObjectKind Kind,
    long Length,
    DateTimeOffset LastWriteTimeUtc);

public sealed record StorageWriteTarget(
    string RootId,
    string RelativePath,
    string FullPath,
    string ParentPath);

public interface IStoragePathResolver
{
    StorageObjectMetadata ResolveExisting(string objectId);

    StorageWriteTarget ResolveWriteTarget(string objectId);
}

public sealed class StorageRootCatalog
{
    private static readonly StringComparer RootIdComparer = StringComparer.Ordinal;
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    private readonly IReadOnlyDictionary<string, AllowedStorageRoot> _roots;

    public IReadOnlyCollection<AllowedStorageRoot> Roots => _roots.Values.ToArray();

    public StorageRootCatalog(IOptions<FileStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var configuredRoots = options.Value.Roots ?? [];
        if (configuredRoots.Length == 0)
        {
            throw new StorageRootConfigurationException("FileStorage:Roots deve conter pelo menos uma raiz.");
        }

        var roots = new Dictionary<string, AllowedStorageRoot>(RootIdComparer);
        foreach (var configuredRoot in configuredRoots)
        {
            if (configuredRoot is null || !IsValidRootId(configuredRoot.Id))
            {
                throw new StorageRootConfigurationException("Cada raiz de armazenamento deve possuir um ID logico valido.");
            }

            if (!roots.TryAdd(configuredRoot.Id, CreateRoot(configuredRoot)))
            {
                throw new StorageRootConfigurationException("Os IDs das raizes de armazenamento devem ser unicos.");
            }
        }

        _roots = roots;
    }

    public AllowedStorageRoot Get(string rootId)
    {
        if (!IsValidRootId(rootId))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.InvalidRootId);
        }

        if (!_roots.TryGetValue(rootId, out var root))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.UnknownRoot);
        }

        return root;
    }

    private static AllowedStorageRoot CreateRoot(AllowedStorageRootOptions configuredRoot)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new StorageRootConfigurationException("ApiFileStorage requer Windows para aplicar a politica de handles e reparse points.");
        }

        if (string.IsNullOrWhiteSpace(configuredRoot.Path)
            || IsUncOrDevicePath(configuredRoot.Path)
            || !Path.IsPathFullyQualified(configuredRoot.Path))
        {
            throw new StorageRootConfigurationException("A raiz de armazenamento deve ser um caminho local absoluto configurado no servidor.");
        }

        string fullPath;
        try
        {
            fullPath = NormalizeComparablePath(Path.GetFullPath(configuredRoot.Path));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            throw new StorageRootConfigurationException("A raiz de armazenamento possui sintaxe invalida.");
        }

        if (!Directory.Exists(fullPath))
        {
            throw new StorageRootConfigurationException("A raiz de armazenamento configurada nao existe ou nao e um diretorio.");
        }

        OpenedStorageHandle openedRoot;
        try
        {
            openedRoot = WindowsFileSystem.OpenAbsolute(fullPath);
        }
        catch (StoragePathRejectedException)
        {
            throw new StorageRootConfigurationException("A raiz de armazenamento nao pode ser canonizada com seguranca.");
        }
        catch (StorageAccessDeniedException)
        {
            throw new StorageRootConfigurationException("O processo nao possui ACL suficiente para a raiz de armazenamento.");
        }
        catch (StorageObjectNotFoundException)
        {
            throw new StorageRootConfigurationException("A raiz de armazenamento deixou de existir durante a validacao.");
        }

        using (openedRoot)
        {
            if (!openedRoot.Information.IsDirectory)
            {
                throw new StorageRootConfigurationException("A raiz de armazenamento deve ser um diretorio.");
            }

            var canonicalPath = NormalizeComparablePath(openedRoot.FinalPath);
            if (!PathComparer.Equals(canonicalPath, fullPath))
            {
                throw new StorageRootConfigurationException("A raiz de armazenamento nao possui resolucao canonica estavel.");
            }

            return new AllowedStorageRoot(configuredRoot.Id, fullPath, canonicalPath);
        }
    }

    private static bool IsValidRootId(string? rootId) =>
        !string.IsNullOrWhiteSpace(rootId)
        && rootId.Length <= 64
        && char.IsAsciiLetterOrDigit(rootId[0])
        && rootId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

    private static bool IsUncOrDevicePath(string path) =>
        path.StartsWith("\\\\", StringComparison.Ordinal)
        || path.StartsWith("//", StringComparison.Ordinal)
        || path.StartsWith("\\\\?\\", StringComparison.Ordinal)
        || path.StartsWith("\\\\.\\", StringComparison.Ordinal)
        || path.StartsWith("\\??\\", StringComparison.Ordinal);

    internal static string NormalizeComparablePath(string path)
    {
        var normalizedPath = path.StartsWith("\\\\?\\UNC\\", StringComparison.OrdinalIgnoreCase)
            ? "\\\\" + path[8..]
            : path.StartsWith("\\\\?\\", StringComparison.OrdinalIgnoreCase)
                ? path[4..]
                : path;

        var fullPath = Path.GetFullPath(normalizedPath);
        var root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrEmpty(root) || fullPath.Length <= root.Length)
        {
            return fullPath;
        }

        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    internal static bool IsWithinRoot(string rootPath, string candidatePath)
    {
        var normalizedRoot = NormalizeComparablePath(rootPath);
        var normalizedCandidate = NormalizeComparablePath(candidatePath);
        if (PathComparer.Equals(normalizedRoot, normalizedCandidate))
        {
            return true;
        }

        var rootWithSeparator = normalizedRoot.EndsWith(Path.DirectorySeparatorChar)
            ? normalizedRoot
            : normalizedRoot + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class SecureStoragePathResolver(StorageRootCatalog rootCatalog) : IStoragePathResolver
{
    private readonly StorageRootCatalog _rootCatalog = rootCatalog ?? throw new ArgumentNullException(nameof(rootCatalog));

    public StorageObjectMetadata ResolveExisting(string objectId)
    {
        var logicalObject = StorageObjectId.Decode(objectId);
        var root = _rootCatalog.Get(logicalObject.RootId);
        var segments = LogicalPathValidator.Validate(logicalObject.RelativePath);
        if (StorageInternalPathPolicy.IsReserved(segments))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.ReservedPath);
        }
        var expectedPath = BuildExpectedPath(root.CanonicalPath, segments);

        if (!StorageRootCatalog.IsWithinRoot(root.CanonicalPath, expectedPath))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.RootBoundary);
        }

        using var openedObject = WindowsFileSystem.OpenRelative(root.CanonicalPath, segments);
        var finalPath = StorageRootCatalog.NormalizeComparablePath(openedObject.FinalPath);

        if (!StorageRootCatalog.IsWithinRoot(root.CanonicalPath, finalPath))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.RootBoundary);
        }

        if (!string.Equals(finalPath, StorageRootCatalog.NormalizeComparablePath(expectedPath), StringComparison.OrdinalIgnoreCase))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.CanonicalizationFailure);
        }

        var canonicalRelativePath = Path.GetRelativePath(root.CanonicalPath, finalPath)
            .Replace(Path.DirectorySeparatorChar, '/');

        return new StorageObjectMetadata(
            root.Id,
            logicalObject.RelativePath,
            canonicalRelativePath,
            openedObject.Information.IsDirectory ? StorageObjectKind.Directory : StorageObjectKind.File,
            openedObject.Information.IsDirectory ? 0 : openedObject.Information.Length,
            openedObject.Information.LastWriteTimeUtc);
    }

    public StorageWriteTarget ResolveWriteTarget(string objectId)
    {
        var logicalObject = StorageObjectId.Decode(objectId);
        var root = _rootCatalog.Get(logicalObject.RootId);
        var segments = LogicalPathValidator.Validate(logicalObject.RelativePath);
        if (StorageInternalPathPolicy.IsReserved(segments))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.ReservedPath);
        }

        var expectedPath = BuildExpectedPath(root.CanonicalPath, segments);
        if (!StorageRootCatalog.IsWithinRoot(root.CanonicalPath, expectedPath))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.RootBoundary);
        }

        var parentSegments = segments.Take(segments.Count - 1).ToArray();
        var parentPath = BuildExpectedPath(root.CanonicalPath, parentSegments);
        if (!StorageRootCatalog.IsWithinRoot(root.CanonicalPath, parentPath))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.RootBoundary);
        }

        WindowsFileSystem.ValidateExistingPrefix(root.CanonicalPath, segments);

        return new StorageWriteTarget(root.Id, logicalObject.RelativePath, expectedPath, parentPath);
    }

    private static string BuildExpectedPath(string rootPath, IReadOnlyList<string> segments)
    {
        try
        {
            var candidatePath = rootPath;
            foreach (var segment in segments)
            {
                candidatePath = Path.Combine(candidatePath, segment);
            }

            return Path.GetFullPath(candidatePath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.InvalidSyntax);
        }
    }
}

public static class StorageObjectId
{
    private const string VersionPrefix = "v1.";
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static string Create(string rootId, string relativePath)
    {
        if (!IsValidRootId(rootId))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.InvalidRootId);
        }

        var segments = LogicalPathValidator.Validate(relativePath);
        var canonicalRelativePath = string.Join('/', segments);
        var payload = rootId + '\0' + canonicalRelativePath;
        return VersionPrefix + ToBase64Url(StrictUtf8.GetBytes(payload));
    }

    public static (string RootId, string RelativePath) Decode(string? objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId)
            || !objectId.StartsWith(VersionPrefix, StringComparison.Ordinal)
            || objectId.Length == VersionPrefix.Length
            || objectId.Length > 12000)
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.InvalidSyntax);
        }

        var encodedPayload = objectId[VersionPrefix.Length..];
        byte[] payloadBytes;
        try
        {
            payloadBytes = FromBase64Url(encodedPayload);
        }
        catch (FormatException)
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.EncodedPath);
        }

        string payload;
        try
        {
            payload = StrictUtf8.GetString(payloadBytes);
        }
        catch (DecoderFallbackException)
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.EncodedPath);
        }

        var separator = payload.IndexOf('\0');
        if (separator <= 0 || separator == payload.Length - 1 || payload.IndexOf('\0', separator + 1) >= 0)
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.InvalidSyntax);
        }

        var rootId = payload[..separator];
        var relativePath = payload[(separator + 1)..];
        if (!IsValidRootId(rootId))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.InvalidRootId);
        }

        _ = LogicalPathValidator.Validate(relativePath);
        var canonicalObjectId = Create(rootId, relativePath);
        if (!string.Equals(canonicalObjectId, objectId, StringComparison.Ordinal))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.EncodedPath);
        }

        return (rootId, relativePath);
    }

    private static bool IsValidRootId(string? rootId) =>
        !string.IsNullOrWhiteSpace(rootId)
        && rootId.Length <= 64
        && char.IsAsciiLetterOrDigit(rootId[0])
        && rootId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        if (value.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_')
            || value.Length % 4 == 1)
        {
            throw new FormatException();
        }

        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        var bytes = Convert.FromBase64String(padded);
        if (!string.Equals(ToBase64Url(bytes), value, StringComparison.Ordinal))
        {
            throw new FormatException();
        }

        return bytes;
    }
}

internal static class LogicalPathValidator
{
    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON",
        "PRN",
        "AUX",
        "NUL",
        "COM1",
        "COM2",
        "COM3",
        "COM4",
        "COM5",
        "COM6",
        "COM7",
        "COM8",
        "COM9",
        "LPT1",
        "LPT2",
        "LPT3",
        "LPT4",
        "LPT5",
        "LPT6",
        "LPT7",
        "LPT8",
        "LPT9"
    };

    public static IReadOnlyList<string> Validate(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.EmptyPath);
        }

        if (relativePath.Length > 4000)
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.InvalidSyntax);
        }

        if (relativePath.Contains('%', StringComparison.Ordinal))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.EncodedPath);
        }

        if (relativePath.Normalize() != relativePath)
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.EncodedPath);
        }

        if (relativePath.StartsWith("\\\\", StringComparison.Ordinal)
            || relativePath.StartsWith("//", StringComparison.Ordinal))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.UncPath);
        }

        if (relativePath.StartsWith("\\\\?\\", StringComparison.Ordinal)
            || relativePath.StartsWith("\\\\.\\", StringComparison.Ordinal)
            || relativePath.StartsWith("\\??\\", StringComparison.Ordinal))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.DevicePath);
        }

        if (Path.IsPathRooted(relativePath)
            || Path.IsPathFullyQualified(relativePath)
            || relativePath.Contains(':', StringComparison.Ordinal))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.AbsolutePath);
        }

        if (relativePath.Any(character => character == '\0'
                || char.IsControl(character)
                || Path.GetInvalidPathChars().Contains(character)))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.InvalidSyntax);
        }

        var segments = relativePath.Split(['\\', '/'], StringSplitOptions.None);
        if (segments.Length == 0 || segments.Any(string.IsNullOrEmpty))
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.InvalidSyntax);
        }

        foreach (var segment in segments)
        {
            if (segment is "." or "..")
            {
                throw new StoragePathRejectedException(StoragePathRejectionReason.Traversal);
            }

            if (segment.Length > 255
                || segment.EndsWith(' ')
                || segment.EndsWith('.')
                || segment.Any(character => character is '"' or '<' or '>' or '|' or ':' or '*' or '?'))
            {
                throw new StoragePathRejectedException(StoragePathRejectionReason.InvalidSyntax);
            }

            var deviceName = segment.Split('.', 2)[0].TrimEnd(' ', '.');
            if (ReservedDeviceNames.Contains(deviceName))
            {
                throw new StoragePathRejectedException(StoragePathRejectionReason.InvalidSyntax);
            }
        }

        return segments;
    }
}

internal static class StorageInternalPathPolicy
{
    public const string QuarantineDirectoryName = ".dtudo-quarantine";
    public const string TrashDirectoryName = ".dtudo-trash";

    public static bool IsReserved(IReadOnlyList<string> segments)
        => segments.Count > 0
            && (string.Equals(segments[0], QuarantineDirectoryName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(segments[0], TrashDirectoryName, StringComparison.OrdinalIgnoreCase));
}

internal sealed class OpenedStorageHandle : IDisposable
{
    public OpenedStorageHandle(SafeFileHandle handle, NativeFileInformation information, string finalPath)
    {
        Handle = handle;
        Information = information;
        FinalPath = finalPath;
    }

    public SafeFileHandle Handle { get; }

    public NativeFileInformation Information { get; }

    public string FinalPath { get; }

    public void Dispose() => Handle.Dispose();
}

internal readonly record struct NativeFileInformation(
    FileAttributes Attributes,
    long Length,
    uint NumberOfLinks,
    DateTimeOffset LastWriteTimeUtc)
{
    public bool IsDirectory => Attributes.HasFlag(FileAttributes.Directory);

    public bool IsReparsePoint => Attributes.HasFlag(FileAttributes.ReparsePoint);
}

internal static class WindowsFileSystem
{
    private const uint GenericRead = 0x80000000;
    private const uint Synchronize = 0x00100000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint FileShareDelete = 0x00000004;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private const uint FileDirectoryFile = 0x00000001;
    private const uint FileSynchronousIoNonalert = 0x00000020;
    private const uint ObjectAttributesCaseInsensitive = 0x00000040;
    private const uint FileOpen = 0x00000001;
    private const uint StatusObjectNameNotFound = 0xC0000034;
    private const uint StatusObjectPathNotFound = 0xC000003A;
    private const uint StatusNoSuchFile = 0xC000000F;
    private const uint StatusAccessDenied = 0xC0000022;
    private const uint StatusNotADirectory = 0xC0000103;
    private const uint StatusReparsePointEncountered = 0xC000050B;

    public static OpenedStorageHandle OpenAbsolute(string path)
    {
        EnsureWindows();

        var handle = CreateFileW(
            ToExtendedLengthPath(path),
            GenericRead,
            FileShareRead | FileShareWrite,
            IntPtr.Zero,
            FileMode.Open,
            FileFlagBackupSemantics | FileFlagOpenReparsePoint,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            handle.Dispose();
            ThrowWin32Error(error);
        }

        try
        {
            return Inspect(handle);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public static OpenedStorageHandle OpenRelative(string rootPath, IReadOnlyList<string> segments)
    {
        EnsureWindows();

        var current = OpenAbsolute(rootPath);
        try
        {
            for (var index = 0; index < segments.Count; index++)
            {
                var isFinalSegment = index == segments.Count - 1;
                var child = OpenRelative(current.Handle, segments[index], !isFinalSegment);
                current.Dispose();
                current = child;
            }

            return current;
        }
        catch
        {
            current.Dispose();
            throw;
        }
    }

    public static void ValidateExistingPrefix(string rootPath, IReadOnlyList<string> segments)
    {
        EnsureWindows();

        var current = OpenAbsolute(rootPath);
        try
        {
            for (var index = 0; index < segments.Count; index++)
            {
                OpenedStorageHandle child;
                try
                {
                    child = OpenRelative(current.Handle, segments[index], index < segments.Count - 1);
                }
                catch (StorageObjectNotFoundException)
                {
                    return;
                }

                current.Dispose();
                current = child;
            }
        }
        finally
        {
            current.Dispose();
        }
    }

    private static OpenedStorageHandle OpenRelative(SafeFileHandle parent, string segment, bool directoryOnly)
    {
        var segmentPointer = Marshal.StringToHGlobalUni(segment);
        var unicodeStringPointer = Marshal.AllocHGlobal(Marshal.SizeOf<UnicodeString>());

        try
        {
            var unicodeString = new UnicodeString
            {
                Length = checked((ushort)(segment.Length * sizeof(char))),
                MaximumLength = checked((ushort)((segment.Length + 1) * sizeof(char))),
                Buffer = segmentPointer
            };
            Marshal.StructureToPtr(unicodeString, unicodeStringPointer, fDeleteOld: false);

            var objectAttributes = new ObjectAttributes
            {
                Length = Marshal.SizeOf<ObjectAttributes>(),
                RootDirectory = parent.DangerousGetHandle(),
                ObjectName = unicodeStringPointer,
                Attributes = ObjectAttributesCaseInsensitive
            };

            var status = NtCreateFile(
                out var rawHandle,
                GenericRead | Synchronize,
                ref objectAttributes,
                out _,
                IntPtr.Zero,
                0,
                FileShareRead | FileShareWrite,
                FileOpen,
                FileSynchronousIoNonalert | FileFlagOpenReparsePoint | (directoryOnly ? FileDirectoryFile : 0),
                IntPtr.Zero,
                0);

            var unsignedStatus = unchecked((uint)status);
            if (unsignedStatus != 0)
            {
                ThrowNtStatus(unsignedStatus);
            }

            var handle = new SafeFileHandle(rawHandle, ownsHandle: true);
            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw new StoragePathRejectedException(StoragePathRejectionReason.CanonicalizationFailure);
            }

            try
            {
                var opened = Inspect(handle);
                if (directoryOnly && !opened.Information.IsDirectory)
                {
                    opened.Dispose();
                    throw new StoragePathRejectedException(StoragePathRejectionReason.InvalidSyntax);
                }

                return opened;
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(unicodeStringPointer);
            Marshal.FreeHGlobal(segmentPointer);
        }
    }

    private static OpenedStorageHandle Inspect(SafeFileHandle handle)
    {
        var information = ReadInformation(handle);
        if (information.IsReparsePoint)
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.ReparsePoint);
        }

        if (information.NumberOfLinks > 1)
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.HardLink);
        }

        var finalPath = GetFinalPath(handle);
        return new OpenedStorageHandle(handle, information, finalPath);
    }

    private static NativeFileInformation ReadInformation(SafeFileHandle handle)
    {
        if (!GetFileInformationByHandle(handle, out var nativeInformation))
        {
            var error = Marshal.GetLastWin32Error();
            ThrowWin32Error(error);
        }

        var fileSize = ((long)nativeInformation.FileSizeHigh << 32) | nativeInformation.FileSizeLow;
        var lastWriteFileTime = ((long)nativeInformation.LastWriteTime.dwHighDateTime << 32)
            | (uint)nativeInformation.LastWriteTime.dwLowDateTime;
        var lastWriteTime = lastWriteFileTime == 0
            ? DateTimeOffset.UnixEpoch
            : DateTimeOffset.FromFileTime(lastWriteFileTime).ToUniversalTime();

        return new NativeFileInformation(
            (FileAttributes)nativeInformation.FileAttributes,
            fileSize,
            nativeInformation.NumberOfLinks,
            lastWriteTime);
    }

    private static string GetFinalPath(SafeFileHandle handle)
    {
        var capacity = 512;
        while (capacity <= 32768)
        {
            var buffer = new System.Text.StringBuilder(capacity);
            var length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, 0);
            if (length == 0)
            {
                var error = Marshal.GetLastWin32Error();
                ThrowWin32Error(error);
            }

            if (length < buffer.Capacity)
            {
                return buffer.ToString();
            }

            capacity *= 2;
        }

        throw new StoragePathRejectedException(StoragePathRejectionReason.CanonicalizationFailure);
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new StorageRootConfigurationException("ApiFileStorage requer Windows para aplicar a politica de handles e reparse points.");
        }
    }

    private static string ToExtendedLengthPath(string path)
    {
        if (path.StartsWith("\\\\?\\", StringComparison.Ordinal))
        {
            return path;
        }

        if (path.StartsWith("\\\\", StringComparison.Ordinal))
        {
            return "\\\\?\\UNC\\" + path[2..];
        }

        return "\\\\?\\" + path;
    }

    private static void ThrowWin32Error(int error)
    {
        if (error is 2 or 3 or 53 or 123 or 267)
        {
            throw new StorageObjectNotFoundException();
        }

        if (error is 5 or 32)
        {
            throw new StorageAccessDeniedException();
        }

        throw new StoragePathRejectedException(StoragePathRejectionReason.CanonicalizationFailure);
    }

    private static void ThrowNtStatus(uint status)
    {
        if (status is StatusObjectNameNotFound or StatusObjectPathNotFound or StatusNoSuchFile or StatusNotADirectory)
        {
            throw new StorageObjectNotFoundException();
        }

        if (status == StatusAccessDenied)
        {
            throw new StorageAccessDeniedException();
        }

        if (status == StatusReparsePointEncountered)
        {
            throw new StoragePathRejectedException(StoragePathRejectionReason.ReparsePoint);
        }

        throw new StoragePathRejectedException(StoragePathRejectionReason.CanonicalizationFailure);
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        FileMode creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", EntryPoint = "GetFileInformationByHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle fileHandle,
        out ByHandleFileInformation information);

    [DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(
        SafeFileHandle fileHandle,
        System.Text.StringBuilder filePath,
        uint bufferLength,
        uint flags);

    [DllImport("ntdll.dll")]
    private static extern int NtCreateFile(
        out IntPtr fileHandle,
        uint desiredAccess,
        ref ObjectAttributes objectAttributes,
        out IoStatusBlock ioStatusBlock,
        IntPtr allocationSize,
        uint fileAttributes,
        uint shareAccess,
        uint createDisposition,
        uint createOptions,
        IntPtr eaBuffer,
        uint eaLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UnicodeString
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ObjectAttributes
    {
        public int Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoStatusBlock
    {
        public IntPtr Status;
        public IntPtr Information;
    }
}
