using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Versioning;
using ApiFileStorage.Configuration;
using ApiFileStorage.Services;
using Microsoft.Extensions.Options;
using Xunit.Sdk;

namespace ApiFileStorage.Tests;

public sealed class StorageAclTests : IDisposable
{
    private readonly string _temporaryDirectory;

    public StorageAclTests()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), "DtudoFileStorageAclTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);
        File.WriteAllText(Path.Combine(_temporaryDirectory, "protected.txt"), "protected");
    }

    [Fact]
    public void RootWithoutReadAndExecuteAcl_FailsClosedDuringStartupValidation()
    {
        SkipIfNotWindows();

        var directoryInfo = new DirectoryInfo(_temporaryDirectory);
        try
        {
            if (!TryDenyCurrentUser(directoryInfo))
            {
                throw SkipException.ForSkip("A conta de teste nao pode aplicar uma ACL de negacao no Windows.");
            }

            Assert.Throws<StorageRootConfigurationException>(() => new StorageRootCatalog(Options.Create(new FileStorageOptions
            {
                Roots =
                [
                    new AllowedStorageRootOptions
                    {
                        Id = "media",
                        Path = _temporaryDirectory
                    }
                ]
            })));
        }
        finally
        {
            RestoreCleanupAccess(directoryInfo);
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool TryDenyCurrentUser(DirectoryInfo directoryInfo)
    {
        try
        {
            var security = directoryInfo.GetAccessControl();
            var identity = WindowsIdentity.GetCurrent().Name;
            if (string.IsNullOrWhiteSpace(identity))
            {
                return false;
            }

            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.AddAccessRule(new FileSystemAccessRule(
                identity,
                FileSystemRights.ReadAndExecute | FileSystemRights.ListDirectory,
                AccessControlType.Deny));
            directoryInfo.SetAccessControl(security);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private static void RestoreCleanupAccess(DirectoryInfo directoryInfo)
    {
        var identity = WindowsIdentity.GetCurrent().Name;
        if (string.IsNullOrWhiteSpace(identity))
        {
            return;
        }

        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(
            identity,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));
        directoryInfo.SetAccessControl(security);
    }

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
