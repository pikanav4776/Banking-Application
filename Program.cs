using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using AccountManagement;
using Request;
using Checkbooks;
using System.IO;
using System.Net.Mime;
using BankingData;

using System.Text.RegularExpressions;
using System.ComponentModel.Design;
using System.Text.Json;
using System.Transactions;
using System.Runtime.Serialization;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

public class Program()
{
    public static BankingContext db = new BankingContext();

    #region helpermethods
        static string ReadRequiredLine(string prompt)
        {
            Console.WriteLine(prompt);
            string? input = Console.ReadLine();
            while (input == null)
            {
                Console.WriteLine("Input stream closed unexpectedly. Please try again.");
                input = Console.ReadLine();
            }
            return input;
        }

        const double MaxAmount = 1_000_000_000_000;

        // non-empty text that fits the database column
        static string ReadBoundedLine(string prompt, int maxLength)
        {
            string input = ReadRequiredLine(prompt);
            while(string.IsNullOrWhiteSpace(input) || input.Length > maxLength)
            {
                input = ReadRequiredLine($"Please enter between 1 and {maxLength} characters.");
            }
            return input;
        }

        // needs exactly one '@' with text before and after it, and no spaces
        public static bool IsValidEmail(string email)
        {
            int at = email.IndexOf('@');
            return at > 0 && at == email.LastIndexOf('@') && at < email.Length - 1 && !email.Contains(' ');
        }

        static string ReadEmail(string prompt)
        {
            string email = ReadBoundedLine(prompt, 40);
            while(!IsValidEmail(email))
            {
                email = ReadBoundedLine("Invalid email address: it must contain an '@' with text before and after it (e.g. name@example.com). Please try again:", 40);
            }
            return email;
        }

        static double ReadAmount(string prompt, bool allowZero = false)
        {
            string message = allowZero ? "Please enter a valid amount of zero or more." : "Please enter a valid amount greater than zero.";
            string input = ReadRequiredLine(prompt);
            while(true)
            {
                if(double.TryParse(input, out double value) && double.IsFinite(value) && value <= MaxAmount && (allowZero ? value >= 0 : value > 0))
                {
                    return value;
                }
                input = ReadRequiredLine(message);
            }
        }

        static long ReadLong(string prompt)
        {
            string input = ReadRequiredLine(prompt);
            long value;
            while(!long.TryParse(input, out value))
            {
                input = ReadRequiredLine("Please enter a whole number.");
            }
            return value;
        }

        public static void logTransaction(UserAccount acc, string description)
        {
            acc.transactions.Add(new Transactions.Transaction(acc.accName, acc.accountNumber, description));
            db.SaveChanges();
        }

        public static void logServiceRequest(UserAccount acc, string requestType, string description)
        {
            acc.serviceRequests.Add(new ServiceRequest(0, acc.accountNumber, acc.username, requestType, description));
            db.SaveChanges();
        }

        public static bool transfer(UserAccount uAcc)
        {
            string transferAccountName = ReadRequiredLine("Enter the recipient's username:");

            long transferRoutingNumber = ReadLong("Enter the recipient's routing number:");

            long transferAccountNumber = ReadLong("Enter the recipient's account number:");

            UserAccount? destAcc = db.UserAccounts.FirstOrDefault(account =>
                account.username == transferAccountName &&
                account.routingNumber == transferRoutingNumber &&
                account.accountNumber == transferAccountNumber);

            if(destAcc == null)
            {
                Console.WriteLine("No matching account found for that username, routing number, and account number. Transfer cancelled.");
                return false;
            }

            if(destAcc == uAcc)
            {
                Console.WriteLine("You cannot transfer money to your own account. Transfer cancelled.");
                return false;
            }

            double transferAmount = ReadAmount("How much do you want to transfer?", allowZero: true);

            if(transferAmount <= 0)
            {
                Console.WriteLine("Transfer amount must be greater than zero. Transfer cancelled.");
                return false;
            }

            if(transferAmount > uAcc.accBalance)
            {
                Console.WriteLine("Insufficient funds for this transfer. Transfer cancelled.");
                return false;
            }

            uAcc.withdraw(transferAmount);
            destAcc.deposit(transferAmount);
            db.SaveChanges();
            logTransaction(uAcc, $"Transferred {transferAmount} to {destAcc.username}");
            logTransaction(destAcc, $"Received {transferAmount} from {uAcc.username}");
            Console.WriteLine($"Successfully transferred {transferAmount} to {destAcc.username}.");
            return true;
        }
        public static void readLastFiveTransactions(string username, TextWriter? output = null)
        {
            output ??= Console.Out;
            UserAccount? account = db.UserAccounts.FirstOrDefault(ac => ac.username == username);
            if(account == null)
            {
                output.WriteLine("You have not entered the right name.");
            }
            else
            {
                // newest 5 from the database, then flipped so they print oldest to newest
                var transactions = db.TransactionRecords
                    .Where(t => t.accountNumber == account.accountNumber)
                    .OrderByDescending(t => t.DateTime)
                    .ThenByDescending(t => t.id)
                    .Take(5)
                    .ToList();
                transactions.Reverse();

                if(transactions.Count < 5)
                {
                    output.WriteLine($"There are only {transactions.Count} transactions");
                }
                foreach(var transaction in transactions)
                {
                    output.WriteLine($"{transaction.id}, {transaction.accName}, {transaction.accountNumber}, {transaction.DateTime:s}, {transaction.description}");
                }
            }
        }
        public static bool checkPassword(Account acc, string password)
        {
            if(password.Length > 15 || password.Length < 9)
            {
                Console.WriteLine("Password must be between 9 and 15 characters long.");
                return false;
            }
            bool hasUpper = Regex.IsMatch(password, "[A-Z]");
            bool hasLower = Regex.IsMatch(password, "[a-z]");
            bool hasNumber = Regex.IsMatch(password, "[0-9]");
            if (hasUpper && hasLower && hasNumber)
            {
                string pattern = @"[^A-Za-z0-9\s]";
                Regex defaultRegex = new Regex(pattern);
                MatchCollection matches = defaultRegex.Matches(password);
                if(matches.Count > 0)
                {
                    acc.setPassword(password);
                    Console.WriteLine("Password successfully changed!");
                    return true;
                }
                else
                {
                    Console.WriteLine("Password does not have special characters");
                    return false;
                }
            }
            else
            {
                string errorMessage = "";
                if(!hasUpper){
                    errorMessage += "No uppercase characters; ";
                }
                if(!hasLower){
                    errorMessage += "No lowercase characters; ";
                }
                if(!hasNumber){
                    errorMessage += "No numbers; ";
                }
                Console.WriteLine(errorMessage.TrimEnd());
                return false;
            }
        }
    #endregion
    public static void requestCheckbook(UserAccount acc)
    {
        ServiceRequest? mostRecent = db.ServiceRequests
            .Where(sr => sr.accountNumber == acc.accountNumber && sr.requestType == "Checkbook")
            .OrderByDescending(sr => sr.dateSent)
            .FirstOrDefault();

        if(mostRecent != null && mostRecent.accepted == null)
        {
            Console.WriteLine($"You already have a pending checkbook request (Request ID: {mostRecent.serviceRequestId}).");
            return;
        }

        ServiceRequest sr = new ServiceRequest(0, acc.accountNumber, acc.username, "Checkbook", $"{acc.username} requested a new checkbook");
        acc.serviceRequests.Add(sr);
        db.SaveChanges();
        Console.WriteLine($"Your checkbook request has been submitted. Your request ID is {sr.serviceRequestId}.");
    }
    public static void passwordChange(Account acc){
        Console.WriteLine("Your new password:");
        Console.WriteLine("     Must be between 9-15 characters:");
        Console.WriteLine("     Must contain uppercase characters");
        Console.WriteLine("     Must contain lowercase characters");
        Console.WriteLine("     Must contain at least 1 number");

        string newPassword = ReadRequiredLine("     Must contain at least 1 special character; Examples:.!@#");
        checkPassword(acc, newPassword);
        db.SaveChanges();
    }

    public static void customer_options(UserAccount acc)
    {
        int option = 0;
        do
        {
            Console.WriteLine("Customer Options:");
            Console.WriteLine("1:   View Account Details");
            Console.WriteLine("2:   Withdraw");
            Console.WriteLine("3:   Deposit");
            Console.WriteLine("4:   Transfer");
            Console.WriteLine("5:   Read Last 5 Transactions");
            Console.WriteLine("6:   Request Checkbook");
            Console.WriteLine("7:   Change Password");
            Console.WriteLine("8:   Log Out");
            while(!int.TryParse(Console.ReadLine(), out option))
            {
                Console.WriteLine("Please enter a valid numeric option.");
            }
            switch (option)
            {
                case 1:
                    Console.WriteLine("Account Details:");
                    acc.checkAccountDetails();
                    break;
                case 2:
                    double withdrawAmount = ReadAmount("How much do you want to withdraw?");
                    if(withdrawAmount > acc.accBalance)
                    {
                        Console.WriteLine("Insufficient funds for this withdrawal.");
                        break;
                    }
                    acc.withdraw(withdrawAmount);
                    db.SaveChanges();
                    logTransaction(acc, $"Withdrew {withdrawAmount}");
                    logServiceRequest(acc, "withdraw", $"{acc.username} withdrew {withdrawAmount} dollars");
                    Console.WriteLine($"Withdrawal successful. New balance: {acc.accBalance}");
                    break;
                case 3:
                    double depositAmount = ReadAmount("How much do you want to deposit?");
                    acc.deposit(depositAmount);
                    db.SaveChanges();
                    logTransaction(acc, $"Deposited {depositAmount}");
                    logServiceRequest(acc, "deposit", $"{acc.username} deposited {depositAmount} dollars");
                    Console.WriteLine($"Deposit successful. New balance: {acc.accBalance}");
                    break;
                case 4:
                    bool transferSucceeded = transfer(acc);
                    if(transferSucceeded)
                    {
                        logServiceRequest(acc, "transfer", $"{acc.username} completed a transfer");
                    }
                    break;
                case 5:
                    readLastFiveTransactions(acc.username);
                    break;
                case 6:
                    requestCheckbook(acc);
                    break;
                case 7:
                    passwordChange(acc);
                    break;
                case 8:
                    Console.WriteLine("Logging out...");
                    break;
                default:
                    Console.WriteLine("Please enter a valid option (1-8).");
                    break;
            }
        } while (option != 8);
    }
    public static void admin_options(AdminAccount acc)
    {
        int options = 0;
        do
        {
            Console.WriteLine("Admin Options:");
            Console.WriteLine("1:   Create new Account");
            Console.WriteLine("2:   Delete an existing account");
            Console.WriteLine("3:   Change an Account");
            Console.WriteLine("4:   View an Account");
            Console.WriteLine("5:   Reset Customer Password");
            Console.WriteLine("6:   Approve Checkbook Request");
            Console.WriteLine("7:   Log Out");
            while(!int.TryParse(Console.ReadLine(), out options))
            {
                Console.WriteLine("Please enter a valid numeric option.");
            }
            switch(options){
                case 1:
                    Console.WriteLine("Creating new Account");
                    string accType = ReadRequiredLine("Do you want to create a user account or admin account");
                    if(accType.ToLower() == "user")
                    {
                        long routingNumber = Random.Shared.Next(0, 1000000000);
                        long accountNumber;
                        do { accountNumber = Random.Shared.Next(0, 1000000000); } while(db.UserAccounts.Any(u => u.accountNumber == accountNumber));
                        string AccName = ReadBoundedLine("What do you want your username to be?", 25);
                        while(db.Accounts.Any(a => a.username == AccName))
                        {
                            AccName = ReadBoundedLine("That username is already taken. Choose another username:", 25);
                        }

                        double accBalance = ReadAmount("What amount would you like to initially deposit?", allowZero: true);

                        bool isActive = true;
                        string email = ReadEmail("What is your email address?");

                        string HomeAddress = ReadBoundedLine("What is your address?", 70);
                        string ssnInput = ReadRequiredLine("What is your SSN? (9 digits)");
                        while(!Regex.IsMatch(ssnInput, "^[0-9]{9}$"))
                        {
                            ssnInput = ReadRequiredLine("SSN must be exactly 9 digits. What is your SSN?");
                        }
                        long SSN = long.Parse(ssnInput);
                        UserAccount newUser = new UserAccount
                        {
                            username = AccName,
                            accName = AccName,
                            routingNumber = routingNumber,
                            accountNumber = accountNumber,
                            accBalance = accBalance,
                            isActive = isActive,
                            email = email,
                            homeAddress = HomeAddress,
                            SSN = SSN
                        };

                        bool userPasswordSet = false;
                        while(!userPasswordSet)
                        {
                            Console.WriteLine("What do you want your password to be?");
                            Console.WriteLine("     Must be between 9-15 characters:");
                            Console.WriteLine("     Must contain uppercase characters");
                            Console.WriteLine("     Must contain lowercase characters");
                            Console.WriteLine("     Must contain at least 1 number");
                            string newUserPassword = ReadRequiredLine("     Must contain at least 1 special character; Examples:.!@#");
                            userPasswordSet = checkPassword(newUser, newUserPassword);
                        }

                        db.UserAccounts.Add(newUser);
                        db.SaveChanges();
                            Console.WriteLine($"Account successfully created for {newUser.username}. Account Number: {newUser.accountNumber}");
                    }
                    else if(accType.ToLower() == "admin")
                    {
                        string adminUsername = ReadBoundedLine("What do you want the admin username to be?", 25);
                        while(db.Accounts.Any(a => a.username == adminUsername))
                        {
                            adminUsername = ReadBoundedLine("That username is already taken. Choose another username:", 25);
                        }

                        AdminAccount newAdmin = new AdminAccount
                        {
                            username = adminUsername
                        };

                        bool adminPasswordSet = false;
                        while(!adminPasswordSet)
                        {
                            Console.WriteLine("What do you want your password to be?");
                            Console.WriteLine("     Must be between 9-15 characters:");
                            Console.WriteLine("     Must contain uppercase characters");
                            Console.WriteLine("     Must contain lowercase characters");
                            Console.WriteLine("     Must contain at least 1 number");
                            string newAdminPassword = ReadRequiredLine("     Must contain at least 1 special character; Examples:.!@#");
                            adminPasswordSet = checkPassword(newAdmin, newAdminPassword);
                        }

                        db.AdminAccounts.Add(newAdmin);
                        db.SaveChanges();
                        Console.WriteLine($"Admin account successfully created for {newAdmin.username}.");
                    }
                    else
                    {
                        Console.WriteLine("Please enter user or admin");
                    }
                    break;
                case 2:
                    string accName = ReadRequiredLine("Whose account do you want to delete");
                    UserAccount? accToDelete = db.UserAccounts.FirstOrDefault(ua => ua.username == accName);
                    if(accToDelete != null)
                    {
                        db.UserAccounts.Remove(accToDelete);
                        db.SaveChanges();
                        Console.WriteLine($"Account for {accName} has been deleted.");
                    }
                    else
                    {
                        Console.WriteLine($"No account found for {accName}.");
                    }
                    break;
                case 3:
                    string accNameToBeChanged = ReadRequiredLine("Whose account do you want to change?");
                    UserAccount? accToBeEdited = db.UserAccounts.FirstOrDefault(ua => ua.username == accNameToBeChanged);
                    if(accToBeEdited == null)
                    {
                        Console.WriteLine("This user does not exist.");
                        break;
                    }
                    else
                    {
                        string details = ReadRequiredLine("What details do you want to alter?");
                        switch (details)
                        {
                            case "AccName":
                                accToBeEdited.accName = ReadBoundedLine($"What is {accToBeEdited.username}", 80);
                                break;
                            case "activity":
                                accToBeEdited.isActive = !accToBeEdited.isActive;
                                string status = accToBeEdited.isActive ? "active" : "inactive";
                                Console.WriteLine($"{accToBeEdited.username} is now {status}");
                                break;
                            case "HomeAddress":
                                accToBeEdited.homeAddress = ReadBoundedLine($"What is {accToBeEdited.username}'s new Address", 70);
                                break;
                            default:
                                break;
                        }
                        db.SaveChanges();
                        break;
                    }
                case 4:
                    string accNameToView = ReadRequiredLine("Whose account do you want to view?");
                    UserAccount? accToView = db.UserAccounts.Include(u => u.Checkbook).Include(u => u.serviceRequests).FirstOrDefault(ua => ua.username == accNameToView);
                    if(accToView == null)
                    {
                        Console.WriteLine($"The account for {accNameToView} does not exist");
                        break;
                    }
                    accToView.checkAccountDetails();
                    break;
                case 5:
                    string accNamePswdReset = ReadRequiredLine("Resetting Customer Password");
                    UserAccount? accPswdReset = db.UserAccounts.FirstOrDefault(ua => ua.username == accNamePswdReset);
                    if(accPswdReset == null)
                    {
                        Console.WriteLine($"Account for {accNamePswdReset} does not exist.");
                        break;
                    }
                    passwordChange(accPswdReset);
                    break;
                case 6:
                    string accCheckbookName = ReadRequiredLine("Approve Checkbook Request");
                    UserAccount? accCheckbook = db.UserAccounts.FirstOrDefault(ua => ua.username == accCheckbookName);
                    if(accCheckbook == null)
                    {
                        Console.WriteLine($"Account for {accCheckbookName} does not exist.");
                        break;
                    }
                    ServiceRequest? pendingCheckbookRequest = db.ServiceRequests
                        .Where(sr => sr.accountNumber == accCheckbook.accountNumber && sr.requestType == "Checkbook" && sr.accepted == null)
                        .OrderByDescending(sr => sr.dateSent)
                        .FirstOrDefault();
                    if(pendingCheckbookRequest == null)
                    {
                        Console.WriteLine($"{accCheckbookName} has no pending checkbook request.");
                        break;
                    }
                    pendingCheckbookRequest.approve();
                    accCheckbook.Checkbook ??= new List<Checkbook>();
                    accCheckbook.Checkbook.Add(new Checkbook(0, "", DateTime.Now, 0, accCheckbook.routingNumber, accCheckbook.accountNumber));
                    // isPending unused for now
                    db.SaveChanges();
                    Console.WriteLine($"Checkbook request approved for {accCheckbookName}.");
                    break;
                case 7:
                    Console.WriteLine("Logging out...");
                    break;
                default:
                    Console.WriteLine("Please enter a valid option (1-7).");
                    break;
            }
        } while (options != 7);
    }
    static void Main(string[] args)
    {
        if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BANKING_SSN_KEY")))
        {
            Console.WriteLine("BANKING_SSN_KEY is not set, so SSNs can't be encrypted.");
            Console.WriteLine("Set it, then close and reopen your terminal (and VS Code) and start again:");
            Console.WriteLine("    setx BANKING_SSN_KEY \"a-long-random-secret\"");
            return;
        }

        if(!db.Database.CanConnect())
        {
            Console.WriteLine("Can't reach the BankingApp database. Make sure SQL Server Express is running,");
            Console.WriteLine("then create the tables with:  dotnet ef database update");
            return;
        }

        if(!db.AdminAccounts.Any(a => a.username == "admin"))
        {
            AdminAccount defaultAdmin = new AdminAccount { username = "admin" };
            defaultAdmin.setPassword("Admin123!");
            db.AdminAccounts.Add(defaultAdmin);
            db.SaveChanges();
            Console.WriteLine("Default admin account created - username: admin / password: Admin123!");
        }

        int option = 0;
        while (option != 3)
        {
            Console.WriteLine("Welcome to this banking application!");
            Console.WriteLine("1.Customer");
            Console.WriteLine("2.Admin");
            Console.WriteLine("3.Exit");

            while(!int.TryParse(Console.ReadLine(), out option))
            {
                Console.WriteLine("Please enter a valid numeric option.");
            }

            switch (option)
            {
                case 1:
                    string custUsername = ReadRequiredLine("Please Enter your username");
                    string custPassword = ReadRequiredLine("Please Enter your password");
                    UserAccount? uAcc = db.UserAccounts.Include(u => u.Checkbook).Include(u => u.serviceRequests).FirstOrDefault(u => u.username == custUsername);
                    if(uAcc == null || !uAcc.verifyPassword(custPassword))
                    {
                        Console.WriteLine("Invalid Credentials");
                        break;
                    }
                    if(!uAcc.isActive)
                    {
                        Console.WriteLine("This account is inactive. Please contact an admin.");
                        break;
                    }
                    customer_options(uAcc);
                    break;
                case 2:
                    string adminUsername = ReadRequiredLine("Please Enter your username");
                    string adminPassword = ReadRequiredLine("Please Enter your password");
                    AdminAccount? aAcc = db.AdminAccounts.FirstOrDefault(a => a.username == adminUsername);
                    if(aAcc == null || !aAcc.verifyPassword(adminPassword))
                    {
                        Console.WriteLine("Invalid Credentials");
                        break;
                    }
                    admin_options(aAcc);
                    break;
                case 3:
                    break;
                default:
                    Console.WriteLine("Please let us know what value you want");
                    break;
            }
        }
    }
}