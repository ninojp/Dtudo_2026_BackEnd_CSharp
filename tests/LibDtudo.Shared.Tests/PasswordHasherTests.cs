using LibDtudo.Shared.Utils;

namespace LibDtudo.Shared.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_DoesNotStorePlainText()
    {
        var hash = PasswordHasher.HashPassword("SenhaForte123!");

        Assert.DoesNotContain("SenhaForte123!", hash, StringComparison.Ordinal);
        Assert.StartsWith("PBKDF2-SHA256.", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyPassword_AcceptsCorrectPassword()
    {
        var hash = PasswordHasher.HashPassword("SenhaForte123!");

        Assert.True(PasswordHasher.VerifyPassword("SenhaForte123!", hash));
    }

    [Fact]
    public void VerifyPassword_RejectsWrongPassword()
    {
        var hash = PasswordHasher.HashPassword("SenhaForte123!");

        Assert.False(PasswordHasher.VerifyPassword("senha-errada", hash));
    }
}
