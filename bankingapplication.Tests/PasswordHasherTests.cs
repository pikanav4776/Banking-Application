using AccountManagement;
using Xunit;

namespace bankingapplication.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void HashThenVerify_SamePassword_ReturnsTrue()
    {
        var stored = PasswordHasher.HashPassword("Passw0rd!x");

        Assert.True(PasswordHasher.VerifyPassword("Passw0rd!x", stored));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var stored = PasswordHasher.HashPassword("Passw0rd!x");

        Assert.False(PasswordHasher.VerifyPassword("Passw0rd!y", stored));
    }

    [Fact]
    public void Hash_DoesNotContainPlaintext_AndUsesAFreshSaltEachTime()
    {
        var first = PasswordHasher.HashPassword("Passw0rd!x");
        var second = PasswordHasher.HashPassword("Passw0rd!x");

        Assert.DoesNotContain("Passw0rd!x", first);
        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Admin123!")]          // legacy plain text, no delimiter
    [InlineData("abcDEF1.x")]           // one delimiter but not base64
    [InlineData("kriB.781.3")]          // too many segments
    [InlineData("AAAA.AAAA")]           // valid base64, wrong lengths
    public void Verify_MalformedStoredValue_ReturnsFalse_InsteadOfThrowing(string stored)
    {
        Assert.False(PasswordHasher.VerifyPassword("Admin123!", stored));
    }

    [Fact]
    public void Account_SetPassword_ThenVerifyPassword()
    {
        var admin = new AdminAccount { username = "admin" };

        admin.setPassword("Admin123!");

        Assert.True(admin.verifyPassword("Admin123!"));
        Assert.False(admin.verifyPassword("admin123!"));
    }
}
