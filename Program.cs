using AccountManagement;
using Request;
using Checkbooks;
using BankingData;

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using System.Text;

public class Program()
{
    public static BankingContext db = new BankingContext();
    const decimal MaxDeposit = 1_000_000_000;

    #region helpermethods
        static string ReadRequiredLine(string prompt, bool secret = false)
        {
            Console.WriteLine(prompt);
            if(secret)
            {
                string censoredInput = censorInput(new StringBuilder());
                Console.WriteLine();
                return censoredInput;
            }

            string? input = Console.ReadLine();
            while (input == null)
            {
                Console.WriteLine("Input stream closed unexpectedly. Please try again.");
                input = Console.ReadLine();
            }
            return input;
        }


        // non-empty text that fits the database column
        static string ReadBoundedLine(string prompt, int maxLength)
        {
            string input = ReadRequiredLine(prompt, false);
            while(string.IsNullOrWhiteSpace(input) || input.Length > maxLength)
            {
                input = ReadRequiredLine($"Please enter between 1 and {maxLength} characters.", false);
            }
            return input;
        }

        public static bool emailValidityCheck(string email)
        {
            int at = email.IndexOf('@');
            int dot = email.IndexOf('.');
            bool atCheck = at > 0 && at == email.LastIndexOf('@') && at < email.Length - 1 && !email.Contains(' ');
            bool dotCheck = dot > at + 1 && dot == email.LastIndexOf('.') && dot < email.Length - 1 && !email.Contains(' ');
            return atCheck && dotCheck;
        }

        static string ReadEmail(string prompt)
        {
            string email = ReadBoundedLine(prompt, 254);
            while(!emailValidityCheck(email))
            {
                email = ReadBoundedLine("Invalid email address: it must contain an '@' with text before and after it (e.g. name@example.com). Please try again:", 254);
            }
            return email;
        }

        static decimal? ReadAmount(string prompt, decimal limit, string context, bool allowZero = false)
        {
            string input = ReadRequiredLine(prompt);
            if(!decimal.TryParse(input, out decimal value))
            {
                Console.WriteLine("That is not a valid number. The request has been cancelled.");
                return null;
            }
            if(value != Math.Round(value, 2))
            {
                Console.WriteLine("Amounts can have at most 2 decimal places. The request has been cancelled.");
                return null;
            }
            if(value < 0 || (value == 0 && !allowZero))
            {
                Console.WriteLine($"{context} must be a positive number. The request has been cancelled.");
                return null;
            }
            if(value > limit)
            {
                Console.WriteLine($"{context} cannot be more than {limit:F2}. The request has been cancelled.");
                return null;
            }
            return value;
        }

        static long ReadLong(string prompt)
        {
            string input = ReadRequiredLine(prompt, false);
            long value;
            while(!long.TryParse(input, out value))
            {
                input = ReadRequiredLine("Please enter a whole number.", false);
            }
            return value;
        }

        // asks for a password until it meets the rules, then sets it on the account (the caller saves)
        static void setNewPassword(Account acc)
        {
            bool passwordSet = false;
            while(!passwordSet)
            {
                Console.WriteLine("What do you want the password to be?");
                Console.WriteLine("     Must be between 9-15 characters:");
                Console.WriteLine("     Must contain uppercase characters");
                Console.WriteLine("     Must contain lowercase characters");
                Console.WriteLine("     Must contain at least 1 number");
                string newPassword = ReadRequiredLine("     Must contain at least 1 special character; Examples:.!@#", true);
                passwordSet = checkPassword(acc, newPassword);
            }
        }

        public static bool transfer(UserAccount uAcc)
        {
            string transferAccountName = ReadRequiredLine("Enter the recipient's username:");

            long transferRoutingNumber = ReadLong("Enter the recipient's routing number:");

            long transferAccountNumber = ReadLong("Enter the recipient's account number:");

            UserAccount? destAcc = db.UserAccounts.FirstOrDefault(account => account.username == transferAccountName &&
            account.routingNumber == transferRoutingNumber && account.accountNumber == transferAccountNumber);

            if(destAcc == null)
            {
                Console.WriteLine("The account you are interested in transferring your money to either doesn't exist or you have the wrong credentials");
                return false;
            }
            if(!destAcc.isActive)
            {
                Console.WriteLine("That account is inactive and can't receive transfers.");
                return false;
            }
            if(destAcc.accountNumber == uAcc.accountNumber)
            {
                Console.WriteLine("Sorry; you can not transfer money into your own account.");
                return false;
            }

            decimal? transferAmount = ReadAmount("How much do you want to transfer?", uAcc.accBalance, "Transfer amount");
            if(transferAmount == null)
            {
                return false;
            }
            decimal amount = transferAmount.Value;

            // balances and both transaction records are saved together so a transfer can't half-complete
            uAcc.withdraw(amount);
            destAcc.deposit(amount);
            uAcc.transactions.Add(new Transactions.Transaction(uAcc.accName, uAcc.accountNumber, $"Transferred {amount} to {destAcc.username}"));
            destAcc.transactions.Add(new Transactions.Transaction(destAcc.accName, destAcc.accountNumber, $"Received {amount} from {uAcc.username}"));
            db.SaveChanges();

            Console.WriteLine($"Successfully transferred {amount} to {destAcc.username}.");
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
                output.WriteLine("Id, Name, Account Number, Time, Action");
                var transactions = db.TransactionRecords.Where(t => t.accountNumber == account.accountNumber).OrderByDescending(t => t.DateTime)
                    .ThenByDescending(t => t.transactionId).Take(5).ToList();
                transactions.Reverse();

                if(transactions.Count < 5)
                {
                    output.WriteLine($"There are only {transactions.Count} transactions");
                }
                foreach(var transaction in transactions)
                {
                    output.WriteLine($"{transaction.transactionId}, {transaction.accName}, {transaction.accountNumber}, {transaction.DateTime:s}, {transaction.description}");
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
        public static string censorInput(StringBuilder password)
        {
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else if (key.KeyChar != '\u0000') 
                {
                    password.Append(key.KeyChar);
                    Console.Write("*"); 
                }
            }
            return password.ToString();
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

        string newPassword = ReadRequiredLine("     Must contain at least 1 special character; Examples:.!@#", true);
        if(checkPassword(acc, newPassword))
        {
            db.SaveChanges();
            Console.WriteLine("Password successfully changed!");
        }
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
                    decimal? withdrawAmount = ReadAmount("How much do you want to withdraw?", acc.accBalance, "Withdrawal amount");
                    if(withdrawAmount == null)
                    {
                        break;
                    }
                    acc.withdraw(withdrawAmount.Value);
                    acc.transactions.Add(new Transactions.Transaction(acc.accName, acc.accountNumber, $"Withdrew {withdrawAmount.Value}"));
                    db.SaveChanges();
                    Console.WriteLine($"Withdrawal successful. New balance: {acc.accBalance}");
                    break;
                case 3:
                    decimal? depositAmount = ReadAmount("How much do you want to deposit?", MaxDeposit,"Deposit amount");
                    if(depositAmount == null)
                    {
                        break;
                    }
                    acc.deposit(depositAmount.Value);
                    acc.transactions.Add(new Transactions.Transaction(acc.accName, acc.accountNumber, $"Deposited {depositAmount.Value}"));
                    db.SaveChanges();
                    Console.WriteLine($"Deposit successful. New balance: {acc.accBalance}");
                    break;
                case 4:
                    transfer(acc);
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
                    Console.WriteLine("Thank you for banking with us today!");
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
            Console.WriteLine("2:   Close an existing account");
            Console.WriteLine("3:   Change an Account");
            Console.WriteLine("4:   View an Account");
            Console.WriteLine("5:   Reset Customer Password");
            Console.WriteLine("6:   Approve Checkbook Request");
            Console.WriteLine("7:   Log Out");
            while(!int.TryParse(Console.ReadLine(), out options))
            {
                Console.WriteLine("Please enter a number between 1-7");
            }
            switch(options){
                case 1:
                    Console.WriteLine("Creating new Account");
                    string accType = ReadRequiredLine("Do you want to create a user account or admin account",false);
                    if(accType.ToLower() == "user")
                    {
                        long routingNumber = db.UserAccounts.Select(u => u.routingNumber).FirstOrDefault();
                        if(routingNumber == 0)
                        {
                            routingNumber = 123456789;
                        }
                        long accountNumber;
                        do
                        {
                            accountNumber = Random.Shared.NextInt64(1_000_000_000, 10_000_000_000);
                        }
                        while(db.UserAccounts.Any(u => u.accountNumber == accountNumber));
                        string AccName = ReadBoundedLine("What do you want your username to be?", 25);
                        while(db.Accounts.Any(a => a.username == AccName))
                        {
                            AccName = ReadBoundedLine($"{AccName} is already taken. Choose another username:", 25);
                        }

                        decimal? initialDeposit = ReadAmount("What amount would you like to initially deposit?", MaxDeposit,"Initial deposit", allowZero: true);
                        if(initialDeposit == null)
                        {
                            Console.WriteLine("Account creation cancelled.");
                            break;
                        }
                        decimal accBalance = initialDeposit.Value;

                        bool isActive = true;
                        string email = ReadEmail("What is your email address?");

                        string HomeAddress = ReadBoundedLine("What is your address? (e.g. 1234 Street Name, City, State, Zip code)", 70);
                        string ssnInput = ReadRequiredLine("What is your SSN? (9 digits)", true);
                        while(!Regex.IsMatch(ssnInput, "^[0-9]{9}$"))
                        {
                            ssnInput = ReadRequiredLine("SSN must be exactly 9 digits. What is your SSN?", true);
                        }
                        UserAccount newUser = new UserAccount
                        {
                            username = AccName,
                            accName = AccName,
                            routingNumber = routingNumber,
                            accountNumber = accountNumber,
                            accBalance = accBalance,
                            isActive = isActive,
                            email = email,
                            homeAddress = HomeAddress
                        };
                        newUser.setSsn(ssnInput);
                        setNewPassword(newUser);

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

                        setNewPassword(newAdmin);

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
                    string accName = ReadRequiredLine("Whose account do you want to close?", false);
                    UserAccount? accToClose = db.UserAccounts.FirstOrDefault(ua => ua.username == accName);
                    if(accToClose != null)
                    {
                        // deactivated rather than removed so its transactions and requests stay on record
                        accToClose.isActive = false;
                        db.SaveChanges();
                        Console.WriteLine($"Account for {accName} has been closed (deactivated) and can no longer log in.");
                    }
                    else
                    {
                        Console.WriteLine($"No account found for {accName}.");
                    }
                    break;
                case 3:
                    string accNameToBeChanged = ReadRequiredLine("Whose account do you want to change?",false);
                    UserAccount? accToBeEdited = db.UserAccounts.FirstOrDefault(ua => ua.username == accNameToBeChanged);
                    if(accToBeEdited == null)
                    {
                        Console.WriteLine("This user does not exist.");
                        break;
                    }
                    else
                    {
                        string details = ReadRequiredLine("What details do you want to alter?",false);
                        switch (details)
                        {
                            case "AccName":
                                accToBeEdited.accName = ReadBoundedLine($"What is {accToBeEdited.username}'s new account name?", 80);
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
                    string accNameToView = ReadRequiredLine("Whose account do you want to view?",false);
                    UserAccount? accToView = db.UserAccounts.Include(u => u.Checkbook).Include(u => u.serviceRequests).FirstOrDefault(ua => ua.username == accNameToView);
                    if(accToView == null)
                    {
                        Console.WriteLine($"The account for {accNameToView} does not exist");
                        break;
                    }
                    accToView.checkAccountDetails();
                    break;
                case 5:
                    string accNamePswdReset = ReadRequiredLine("Resetting Customer Password. Whose password do you want to reset?");
                    UserAccount? accPswdReset = db.UserAccounts.FirstOrDefault(ua => ua.username == accNamePswdReset);
                    if(accPswdReset == null)
                    {
                        Console.WriteLine($"Account for {accNamePswdReset} does not exist.");
                        break;
                    }
                    passwordChange(accPswdReset);
                    break;
                case 6:
                    string accCheckbookName = ReadRequiredLine("Approve Checkbook Request",false);
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
        if(!db.Database.CanConnect())
        {
            Console.WriteLine("Can't reach the BankingApp database. Make sure SQL Server Express is running,");
            Console.WriteLine("then create the tables with:  dotnet ef database update");
            return;
        }

        if(!db.AdminAccounts.Any())
        {
            Console.WriteLine("No admin account exists yet. Let's create the first one.");
            AdminAccount firstAdmin = new AdminAccount { username = ReadBoundedLine("What do you want the admin username to be?", 25) };
            setNewPassword(firstAdmin);
            db.AdminAccounts.Add(firstAdmin);
            db.SaveChanges();
            Console.WriteLine($"Admin account created for {firstAdmin.username}.");
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
                    string userUsername = ReadRequiredLine("Please enter your username", false);
                    string userPassword = ReadRequiredLine("Please enter your password", true);
                    UserAccount? uAcc = db.UserAccounts.Include(u => u.Checkbook).Include(u => u.serviceRequests).FirstOrDefault(u => u.username == userUsername);
                    if(uAcc == null || !uAcc.verifyPassword(userPassword))
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
                    string adminUsername = ReadRequiredLine("Please enter your username", false);
                    string adminPassword = ReadRequiredLine("Please enter your password", true);
                    AdminAccount? aAcc = db.AdminAccounts.FirstOrDefault(a => a.username == adminUsername);
                    if(aAcc == null || !aAcc.verifyPassword(adminPassword))
                    {
                        Console.WriteLine("Invalid Credentials");
                        break;
                    }
                    admin_options(aAcc);
                    break;
                case 3:
                    Console.WriteLine("Thank you for using this application!");
                    break;
                default:
                    Console.WriteLine("Please indicate whether you want to access this tool as a customer or admin by typing 1 or 2, respectively.");
                    break;
            }
        }
    }
}