using Xunit;

namespace bankingapplication.Tests;

public class EmailValidationTests
{
    [Theory]
    [InlineData("name@example.com")]
    [InlineData("a@b")]
    [InlineData("first.last+tag@sub.example.co")]
    public void EmailsWithAnAtSign_AreAccepted(string email)
    {
        Assert.True(Program.IsValidEmail(email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-at-sign.example.com")]
    [InlineData("@")]
    [InlineData("@example.com")]
    [InlineData("name@")]
    [InlineData("two@@example.com")]
    [InlineData("a@b@c.com")]
    [InlineData("has space@example.com")]
    public void EmailsWithoutAProperAtSign_AreRejected(string email)
    {
        Assert.False(Program.IsValidEmail(email));
    }
}
