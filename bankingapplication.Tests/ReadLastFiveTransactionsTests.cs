using AccountManagement;
using BankingData;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace bankingapplication.Tests;

public class ReadLastFiveTransactionsTests : IDisposable
{
    private const string TestConnectionString =
        "Server=localhost\\SQLEXPRESS;Database=BankingAppTests;Trusted_Connection=True;TrustServerCertificate=True";

    private static readonly object SchemaLock = new();
    private static bool _schemaCreated;

    public ReadLastFiveTransactionsTests()
    {
        // The SSN column is encrypted, so a key must exist even though the tests never read it back.
        if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BANKING_SSN_KEY")))
        {
            Environment.SetEnvironmentVariable("BANKING_SSN_KEY", "test-only-key");
        }

        var options = new DbContextOptionsBuilder<BankingContext>().UseSqlServer(TestConnectionString).Options;
        Program.db = new BankingContext(options);

        lock(SchemaLock)
        {
            if(!_schemaCreated)
            {
                Program.db.Database.EnsureDeleted();
                Program.db.Database.EnsureCreated();
                _schemaCreated = true;
            }
        }
        Program.db.Accounts.ExecuteDelete();
    }

    public void Dispose()
    {
        Program.db.Accounts.ExecuteDelete();
        Program.db.Dispose();
    }

    private static UserAccount AddUser(string username, string accName)
    {
        var acc = new UserAccount
        {
            username = username,
            accName = accName,
            accountNumber = 1234,
            email = "a@b.c",
            homeAddress = "x",
            SSN = 0L
        };
        Program.db.UserAccounts.Add(acc);
        Program.db.SaveChanges();
        return acc;
    }

    private static string Read(string username)
    {
        var w = new StringWriter();
        Program.readLastFiveTransactions(username, w);
        return w.ToString();
    }

    private static string[] Lines(string output) =>
        output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

    [Fact]
    public void UnknownUser_PrintsError_AndRecordsNothing()
    {
        var output = Read("nobody");

        Assert.Contains("You have not entered the right name.", output);
        Assert.Empty(Program.db.TransactionRecords);
    }

    [Fact]
    public void NoTransactions_ReportsZero()
    {
        AddUser("pranav", "pranav");

        var lines = Lines(Read("pranav"));

        Assert.Single(lines);
        Assert.Equal("There are only 0 transactions", lines[0]);
    }

    [Fact]
    public void ThreeTransactions_ReportsThree_AndPrintsAll()
    {
        var acc = AddUser("pranav", "pranav");
        for (int i = 1; i <= 3; i++) Program.logTransaction(acc, $"tx{i}");

        var lines = Lines(Read("pranav"));

        Assert.Equal("There are only 3 transactions", lines[0]);
        Assert.Equal(4, lines.Length);
        Assert.EndsWith("tx1", lines[1]);
        Assert.EndsWith("tx3", lines[3]);
    }

    [Fact]
    public void ExactlyFiveTransactions_PrintsAllFive_WithoutWarning()
    {
        var acc = AddUser("pranav", "pranav");
        for (int i = 1; i <= 5; i++) Program.logTransaction(acc, $"tx{i}");

        var output = Read("pranav");

        Assert.DoesNotContain("only", output);
        Assert.Equal(5, Lines(output).Length);
    }

    [Fact]
    public void EightTransactions_PrintsOnlyLastFive_Oldest_ToNewest()
    {
        var acc = AddUser("pranav", "pranav");
        for (int i = 1; i <= 8; i++) Program.logTransaction(acc, $"tx{i}");

        var lines = Lines(Read("pranav"));

        Assert.Equal(5, lines.Length);
        Assert.EndsWith("tx4", lines[0]);
        Assert.EndsWith("tx8", lines[4]);
        Assert.DoesNotContain(lines, l => l.EndsWith("tx3"));
    }

    [Fact]
    public void AccNameDifferentFromUsername_StillWorks()
    {
        var acc = AddUser("pranav", "Pranav Madan (edited)");
        Program.logTransaction(acc, "deposit");

        var output = Read("pranav");

        Assert.DoesNotContain("right name", output);
        Assert.Contains("deposit", output);
    }

    [Fact]
    public void OtherUsersTransactions_AreNotShown()
    {
        var pranav = AddUser("pranav", "pranav");
        var other = new UserAccount
        {
            username = "other", accName = "other", accountNumber = 9999,
            email = "o@b.c", homeAddress = "y", SSN = 0L
        };
        Program.db.UserAccounts.Add(other);
        Program.db.SaveChanges();

        Program.logTransaction(pranav, "mine");
        Program.logTransaction(other, "theirs");

        var output = Read("pranav");

        Assert.Contains("mine", output);
        Assert.DoesNotContain("theirs", output);
    }

    [Fact]
    public void LogTransaction_AssignsIncrementingIds()
    {
        var acc = AddUser("pranav", "pranav");
        Program.logTransaction(acc, "a");
        Program.logTransaction(acc, "b");

        var ids = Program.db.TransactionRecords.OrderBy(t => t.id).Select(t => t.id).ToList();

        Assert.Equal(2, ids.Count);
        Assert.True(ids[0] < ids[1]);
    }
}
