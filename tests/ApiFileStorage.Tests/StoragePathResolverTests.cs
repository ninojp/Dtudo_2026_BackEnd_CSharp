using System.Runtime.InteropServices;
using ApiFileStorage.Configuration;
using ApiFileStorage.Services;
using Microsoft.Extensions.Options;
using Xunit.Sdk;

namespace ApiFileStorage.Tests;

public sealed class StoragePathResolverTests : IDisposable
{
    private readonly string _temporaryDirectory;

    public StoragePathResolverTests()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), "DtudoFileStorageTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);
        Directory.CreateDirectory(Path.Combine(_temporaryDirectory, "nested"));
        File.WriteAllText(Path.Combine(_temporaryDirectory, "nested", "safe.txt"), "safe");
    }

    [Fact]
    public void ExistingFile_ResolvesToLogicalCanonicalMetadata()
    {
        SkipIfNotWindows();

        var resolver = CreateResolver();

        var metadata = resolver.ResolveExisting(StorageObjectId.Create("media", "nested/safe.txt"));

        Assert.Equal("media", metadata.RootId);
        Assert.Equal("nested/safe.txt", metadata.CanonicalRelativePath);
        Assert.Equal(StorageObjectKind.File, metadata.Kind);
        Assert.Equal(4, metadata.Length);
        Assert.DoesNotContain(_temporaryDirectory, metadata.CanonicalRelativePath, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("C:\\outside.txt", StoragePathRejectionReason.AbsolutePath)]
    [InlineData("\\\\server\\share\\outside.txt", StoragePathRejectionReason.UncPath)]
    [InlineData("\\\\?\\C:\\outside.txt", StoragePathRejectionReason.UncPath)]
    [InlineData("..\\outside.txt", StoragePathRejectionReason.Traversal)]
    [InlineData("nested\\..\\safe.txt", StoragePathRejectionReason.Traversal)]
    [InlineData("nested/%2e%2e/safe.txt", StoragePathRejectionReason.EncodedPath)]
    [InlineData("nested/%252e%252e/safe.txt", StoragePathRejectionReason.EncodedPath)]
    [InlineData("nested\\safe.txt:secret", StoragePathRejectionReason.AbsolutePath)]
    public void MaliciousLogicalPaths_AreRejected(string relativePath, StoragePathRejectionReason expectedReason)
    {
        SkipIfNotWindows();

        var resolver = CreateResolver();

        var exception = Assert.Throws<StoragePathRejectedException>(
            () => resolver.ResolveExisting(UncheckedStorageObjectId.Create("media", relativePath)));

        Assert.Equal(expectedReason, exception.Reason);
    }

    [Fact]
    public void UnknownOrMalformedRootId_IsRejected()
    {
        SkipIfNotWindows();

        var resolver = CreateResolver();

        Assert.Equal(
            StoragePathRejectionReason.UnknownRoot,
            Assert.Throws<StoragePathRejectedException>(() => resolver.ResolveExisting(StorageObjectId.Create("unknown", "nested/safe.txt"))).Reason);
        Assert.Equal(
            StoragePathRejectionReason.InvalidRootId,
            Assert.Throws<StoragePathRejectedException>(() => resolver.ResolveExisting(UncheckedStorageObjectId.Create("../media", "nested/safe.txt"))).Reason);
    }

    [Fact]
    public void MissingObject_IsNotConfusedWithAValidResolution()
    {
        SkipIfNotWindows();

        var resolver = CreateResolver();

        Assert.Throws<StorageObjectNotFoundException>(
            () => resolver.ResolveExisting(StorageObjectId.Create("media", "nested/missing.txt")));
    }

    [RequiresSymbolicLinkFact]
    public void SymbolicLink_IsRejectedBeforeResolution()
    {
        SkipIfNotWindows();

        var outsideFile = Path.Combine(Path.GetTempPath(), $"DtudoFileStorageOutside-{Guid.NewGuid():N}.txt");
        var linkPath = Path.Combine(_temporaryDirectory, "nested", "symbolic-link.txt");
        File.WriteAllText(outsideFile, "outside");
        try
        {
            File.CreateSymbolicLink(linkPath, outsideFile);

            var rejection = Assert.Throws<StoragePathRejectedException>(
                () => CreateResolver().ResolveExisting(StorageObjectId.Create("media", "nested/symbolic-link.txt")));

            Assert.Equal(StoragePathRejectionReason.ReparsePoint, rejection.Reason);

            var writeRejection = Assert.Throws<StoragePathRejectedException>(
                () => CreateResolver().ResolveWriteTarget(StorageObjectId.Create("media", "nested/symbolic-link.txt")));

            Assert.Equal(StoragePathRejectionReason.ReparsePoint, writeRejection.Reason);
        }
        finally
        {
            File.Delete(linkPath);
            File.Delete(outsideFile);
        }
    }

    [Fact]
    public void Junction_IsRejectedBeforeResolution()
    {
        SkipIfNotWindows();

        var outsideDirectory = Path.Combine(Path.GetTempPath(), $"DtudoFileStorageOutside-{Guid.NewGuid():N}");
        var junctionPath = Path.Combine(_temporaryDirectory, "junction");
        Directory.CreateDirectory(outsideDirectory);
        File.WriteAllText(Path.Combine(outsideDirectory, "outside.txt"), "outside");
        try
        {
            if (!TryCreateJunction(junctionPath, outsideDirectory))
            {
                throw SkipException.ForSkip("A conta de teste nao conseguiu criar junction no Windows.");
            }

            var exception = Assert.Throws<StoragePathRejectedException>(
                () => CreateResolver().ResolveExisting(StorageObjectId.Create("media", "junction/outside.txt")));

            Assert.Equal(StoragePathRejectionReason.ReparsePoint, exception.Reason);

            var writeException = Assert.Throws<StoragePathRejectedException>(
                () => CreateResolver().ResolveWriteTarget(StorageObjectId.Create("media", "junction/outside.txt")));

            Assert.Equal(StoragePathRejectionReason.ReparsePoint, writeException.Reason);
        }
        finally
        {
            TryRemoveJunction(junctionPath);
            Directory.Delete(outsideDirectory, recursive: true);
        }
    }

    [Fact]
    public void HardLink_IsRejectedByHandleLinkCount()
    {
        SkipIfNotWindows();

        var sourcePath = Path.Combine(_temporaryDirectory, "nested", "hard-link-source.txt");
        var hardLinkPath = Path.Combine(_temporaryDirectory, "nested", "hard-link-alias.txt");
        File.WriteAllText(sourcePath, "hard-link");
        try
        {
            if (!CreateHardLink(hardLinkPath, sourcePath))
            {
                throw SkipException.ForSkip("A conta de teste nao conseguiu criar hard link no Windows.");
            }

            var exception = Assert.Throws<StoragePathRejectedException>(
                () => CreateResolver().ResolveExisting(StorageObjectId.Create("media", "nested/hard-link-source.txt")));

            Assert.Equal(StoragePathRejectionReason.HardLink, exception.Reason);

            var writeException = Assert.Throws<StoragePathRejectedException>(
                () => CreateResolver().ResolveWriteTarget(StorageObjectId.Create("media", "nested/hard-link-source.txt")));

            Assert.Equal(StoragePathRejectionReason.HardLink, writeException.Reason);
        }
        finally
        {
            File.Delete(hardLinkPath);
        }
    }

    [Fact]
    public async Task ConcurrentRename_NeverReturnsMetadataOutsideTheAllowedRoot()
    {
        SkipIfNotWindows();

        var rootFile = Path.Combine(_temporaryDirectory, "race.txt");
        var movedFile = Path.Combine(Path.GetTempPath(), $"DtudoFileStorageRace-{Guid.NewGuid():N}.txt");
        File.WriteAllText(rootFile, "inside");
        var objectId = StorageObjectId.Create("media", "race.txt");
        using var cancellation = new CancellationTokenSource();
        var swapper = Task.Run(() =>
        {
            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    if (File.Exists(rootFile))
                    {
                        File.Move(rootFile, movedFile, overwrite: true);
                    }

                    if (File.Exists(movedFile))
                    {
                        File.Move(movedFile, rootFile, overwrite: true);
                    }
                }
                catch (IOException)
                {
                }
            }
        });

        try
        {
            var resolver = CreateResolver();
            for (var attempt = 0; attempt < 250; attempt++)
            {
                try
                {
                    var metadata = resolver.ResolveExisting(objectId);
                    Assert.Equal("race.txt", metadata.CanonicalRelativePath);
                }
                catch (StorageObjectNotFoundException)
                {
                }
                catch (StoragePathRejectedException)
                {
                }
                catch (StorageAccessDeniedException)
                {
                }
            }
        }
        finally
        {
            cancellation.Cancel();
            await swapper;
            File.Delete(rootFile);
            File.Delete(movedFile);
        }
    }

    [RequiresSymbolicLinkFact]
    public void RootConfiguration_RejectsReparsePoint()
    {
        SkipIfNotWindows();

        var realRoot = Path.Combine(Path.GetTempPath(), $"DtudoFileStorageRealRoot-{Guid.NewGuid():N}");
        var linkedRoot = Path.Combine(Path.GetTempPath(), $"DtudoFileStorageLinkedRoot-{Guid.NewGuid():N}");
        Directory.CreateDirectory(realRoot);
        var linkedRootCreated = false;
        try
        {
            Directory.CreateSymbolicLink(linkedRoot, realRoot);
            linkedRootCreated = true;

            Assert.Throws<StorageRootConfigurationException>(() => CreateResolver(linkedRoot));
        }
        finally
        {
            if (linkedRootCreated)
            {
                Directory.Delete(linkedRoot, recursive: false);
            }
            Directory.Delete(realRoot, recursive: true);
        }
    }

    private SecureStoragePathResolver CreateResolver(string? rootPath = null)
    {
        var catalog = new StorageRootCatalog(Options.Create(new FileStorageOptions
        {
            Roots =
            [
                new AllowedStorageRootOptions
                {
                    Id = "media",
                    Path = rootPath ?? _temporaryDirectory
                }
            ]
        }));
        return new SecureStoragePathResolver(catalog);
    }

    private static bool TryCreateJunction(string junctionPath, string targetPath)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c mklink /J \"{junctionPath}\" \"{targetPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        process?.WaitForExit();
        return process?.ExitCode == 0 && Directory.Exists(junctionPath);
    }

    private static void TryRemoveJunction(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: false);
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string fileName, string existingFileName, IntPtr securityAttributes);

    private static bool CreateHardLink(string linkPath, string sourcePath)
        => CreateHardLink(linkPath, sourcePath, IntPtr.Zero);

    private static void SkipIfNotWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw SkipException.ForSkip("Os testes da ApiFileStorage exigem Windows.");
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }
}

internal sealed class RequiresSymbolicLinkFactAttribute : FactAttribute
{
    public RequiresSymbolicLinkFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Os testes de symlink da ApiFileStorage exigem Windows.";
        }
        else if (!StorageTestCapabilities.CanCreateSymbolicLink())
        {
            Skip = "A conta de teste nao possui privilegio para criar symlink no Windows.";
        }
    }
}

internal static class StorageTestCapabilities
{
    public static bool CanCreateSymbolicLink()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DtudoFileStorageSymlinkProbe", Guid.NewGuid().ToString("N"));
        var target = Path.Combine(directory, "target.txt");
        var link = Path.Combine(directory, "link.txt");
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(target, "probe");
            File.CreateSymbolicLink(link, target);
            return (File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        finally
        {
            File.Delete(link);
            File.Delete(target);
            Directory.Delete(directory, recursive: false);
        }
    }
}
