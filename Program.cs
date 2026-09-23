using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using AccountManagement;
using Request;
using Checkbooks;
using System.IO;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.ComponentModel.Design;
using System.Text.Json;
using System.Transactions;
using System.Runtime.Serialization;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;

public class Program()
{
    public static List<UserAccount> userAccounts = new List<UserAccount>();
    public static List<AdminAccount> adminAccounts = new List<AdminAccount>();

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

        public static bool transfer(UserAccount uAcc)
        {
            string transferAccountName = ReadRequiredLine("Enter the recipient's username:");

            Console.WriteLine("Enter the recipient's routing number:");
            long transferRoutingNumber = Convert.ToInt64(Console.ReadLine());

            Console.WriteLine("Enter the recipient's account number:");
            long transferAccountNumber = Convert.ToInt64(Console.ReadLine());

            UserAccount? destAcc = userAccounts.FirstOrDefault(account =>
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

            Console.WriteLine("How much do you want to transfer?");
            double transferAmount = Convert.ToDouble(Console.ReadLine());

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
            Console.WriteLine($"Successfully transferred {transferAmount} to {destAcc.username}.");
            return true;
        }
        public static void readLastFiveTransactions(string accountName)
        {
            string? acName = (from ac in userAccounts where ac.username == accountName select ac.AccName).FirstOrDefault();
            if(acName == null)
            {
                Console.Write("You have not entered the right name.");
            }
            else
            {
                /*
                    Approach for MVP: 
                        If no log exists, a log file will be created, it will be called log_{account_name}.txt
                        The properties of the transaction class would be written into the appropriate log file upon creation.
                        If the log already exists for an account, we go to the log(which will be a text file), and enter all the information for that transaction there.
                        A loop will be used to read from the file, whose contents will be aggregated into a collection and read via LINQ.
                        If there are less than 5 transactions, then the program will identify this and read off the existing transactions.
                    In subsequent updates, this will read from the database.
                */
                string currentDirectory = Directory.GetCurrentDirectory();
                string path = currentDirectory + "\\" + "log_" + accountName + ".txt";
                string content = "id, name, accountNumber, DateTime, AccName, Description";
                if(!File.Exists(path))
                {
                    Console.WriteLine("Path:"+path);
                    File.WriteAllText(path, content);
                }

                string[] lines = File.ReadAllLines(path);
                var transactions = new List<string>(lines);

                transactions.RemoveAt(0);
                
                if(transactions.Count() < 5)
                {
                    Console.WriteLine($"There are only {transactions.Count()} transactions");
                }
                foreach(var transaction in transactions)
                {
                    Console.WriteLine(transaction);
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
                string pattern = @"\w[!@#$%^&*():]";
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
    public static void requestCheckbook(List<ServiceRequest> serviceRequests, UserAccount acc)
    {
        ServiceRequest? mostRecent = serviceRequests
            .Where(sr => sr.accountNumber == acc.accountNumber && sr.requestType == "Checkbook")
            .OrderByDescending(sr => sr.dateSent)
            .FirstOrDefault();

        if(mostRecent != null && mostRecent.accepted == null)
        {
            Console.WriteLine($"You already have a pending checkbook request (Request ID: {mostRecent.serviceRequestId}).");
            return;
        }

        int newId = serviceRequests.Count == 0 ? 1 : serviceRequests.Max(sr => sr.serviceRequestId) + 1;
        ServiceRequest sr = new ServiceRequest(newId, acc.accountNumber, acc.username, "Checkbook", $"{acc.username} requested a new checkbook");
        serviceRequests.Add(sr);
        Console.WriteLine($"Your checkbook request has been submitted. Your request ID is {newId}.");
    }
    public static void passwordChange(Account acc){
        string userName = ReadRequiredLine("Username:");
        if(userName == acc.username)
        {
            Console.WriteLine("Your new password:");
            Console.WriteLine("     Must be between 9-15 characters:");
            Console.WriteLine("     Must contain uppercase characters");
            Console.WriteLine("     Must contain lowercase characters");
            Console.WriteLine("     Must contain at least 1 number");

            string newPassword = ReadRequiredLine("     Must contain at least 1 special character; Examples:.!@#");
            checkPassword(acc, newPassword);
        }
        else
        {
            Console.WriteLine("Sorry, your username is incorrect;");
        }
    }

    public static void customer_options(UserAccount acc, List<ServiceRequest> serviceRequests)
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
                    Console.WriteLine("How much do you want to withdraw?");
                    double withdrawAmount;
                    while(!double.TryParse(Console.ReadLine(), out withdrawAmount) || withdrawAmount <= 0)
                    {
                        Console.WriteLine("Please enter a valid amount greater than zero.");
                    }
                    if(withdrawAmount > acc.accBalance)
                    {
                        Console.WriteLine("Insufficient funds for this withdrawal.");
                        break;
                    }
                    acc.withdraw(withdrawAmount);
                    int withdrawId = serviceRequests.Count == 0 ? 1 : serviceRequests.Max(sr => sr.serviceRequestId) + 1;
                    serviceRequests.Add(new ServiceRequest(withdrawId, acc.accountNumber, acc.username, "withdraw", $"{acc.username} withdrew {withdrawAmount} dollars"));
                    Console.WriteLine($"Withdrawal successful. New balance: {acc.accBalance}");
                    break;
                case 3:
                    Console.WriteLine("How much do you want to deposit?");
                    double depositAmount;
                    while(!double.TryParse(Console.ReadLine(), out depositAmount) || depositAmount <= 0)
                    {
                        Console.WriteLine("Please enter a valid amount greater than zero.");
                    }
                    acc.deposit(depositAmount);
                    int depositId = serviceRequests.Count == 0 ? 1 : serviceRequests.Max(sr => sr.serviceRequestId) + 1;
                    serviceRequests.Add(new ServiceRequest(depositId, acc.accountNumber, acc.username, "deposit", $"{acc.username} deposited {depositAmount} dollars"));
                    Console.WriteLine($"Deposit successful. New balance: {acc.accBalance}");
                    break;
                case 4:
                    bool transferSucceeded = transfer(acc);
                    if(transferSucceeded)
                    {
                        int transferId = serviceRequests.Count == 0 ? 1 : serviceRequests.Max(sr => sr.serviceRequestId) + 1;
                        serviceRequests.Add(new ServiceRequest(transferId, acc.accountNumber, acc.username, "transfer", $"{acc.username} completed a transfer"));
                    }
                    break;
                case 5:
                    readLastFiveTransactions(acc.AccName);
                    break;
                case 6:
                    requestCheckbook(serviceRequests, acc);
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
    public static void admin_options(AdminAccount acc, List<ServiceRequest> serviceRequests)
    {
        int options = 0;
        do
        {
        Console.WriteLine("Admin Options:");
        Console.WriteLine("1:   Create new Account:     ");
        Console.WriteLine("2:   Delete an existing account:     ");
        Console.WriteLine("3:   Change an Account:     ");
        Console.WriteLine("4:   View an Account:       ");
        Console.WriteLine("5:   Reset Customer Password:       ");
        Console.WriteLine("6:   Approve Checkbook Request:       ");
        Console.WriteLine("7:   Log Out:       ");
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
                    long accountNumber = Random.Shared.Next(0, 1000000000);
                    string AccName = ReadRequiredLine("What do you want your username to be?");

                    Console.WriteLine("What amount would you like to initially deposit?");
                    double accBalance = Convert.ToDouble(Console.ReadLine());

                    bool isActive = true;
                    string email = ReadRequiredLine("What is your email address?");

                    string HomeAddress = ReadRequiredLine("What is your address?");

                    string SSN = ReadRequiredLine("What is your SSN?");

                    UserAccount newUser = new UserAccount
                    {
                        username = AccName,
                        AccName = AccName,
                        routingNumber = routingNumber,
                        accountNumber = accountNumber,
                        accBalance = accBalance,
                        IsActive = isActive,
                        email = email,
                        HomeAddress = HomeAddress,
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

                    userAccounts.Add(newUser);
                    Console.WriteLine($"Account successfully created for {newUser.username}. Account Number: {newUser.accountNumber}");
                }
                else if(accType.ToLower() == "admin")
                {
                    string adminUsername = ReadRequiredLine("What do you want the admin username to be?");

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

                    adminAccounts.Add(newAdmin);
                    Console.WriteLine($"Admin account successfully created for {newAdmin.username}.");
                }
                else
                {
                    Console.WriteLine("Please enter user or admin");
                }
                break;
            case 2:
                string accName = ReadRequiredLine("Whose account do you want to delete");
                UserAccount? accToDelete = userAccounts.FirstOrDefault(ua => ua.username == accName);
                if(accToDelete != null)
                {
                    userAccounts.Remove(accToDelete);
                    Console.WriteLine($"Account for {accName} has been deleted.");
                }
                else
                {
                    Console.WriteLine($"No account found for {accName}.");
                }
                break;
            case 3:
                string accNameToBeChanged = ReadRequiredLine("Whose account do you want to change?");
                UserAccount? accToBeEdited = userAccounts.FirstOrDefault(ua => string.Equals(ua.username, accNameToBeChanged, StringComparison.OrdinalIgnoreCase));
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
                            accToBeEdited.AccName = ReadRequiredLine($"What is {accToBeEdited.username}");
                            break;
                        case "activity":
                            accToBeEdited.IsActive = !accToBeEdited.IsActive;
                            string status = accToBeEdited.IsActive ? "active" : "inactive";
                            Console.WriteLine($"{accToBeEdited.username} is now {status}");
                            break;
                        case "HomeAddress":
                            accToBeEdited.HomeAddress = ReadRequiredLine($"What is {accToBeEdited.username}'s new Address");
                            break;
                        default:
                            break;
                    }
                    break;
                }
            case 4:
                string accNameToView = ReadRequiredLine("Whose account do you want to view?");
                UserAccount? accToView = userAccounts.FirstOrDefault(ua => ua.username == accNameToView);
                if(accToView == null)
                {
                    Console.WriteLine($"The account for {accNameToView} does not exist");
                    break;
                }
                accToView.checkAccountDetails();
                break;
            case 5:
                string accNamePswdReset = ReadRequiredLine("Resetting Customer Password");
                UserAccount? accPswdReset = userAccounts.FirstOrDefault(ua => ua.username == accNamePswdReset);
                if(accPswdReset == null)
                {
                    Console.WriteLine($"Account for {accNamePswdReset} does not exist.");
                    break;
                }
                passwordChange(accPswdReset);
                break;
            case 6:
                string accCheckbookName = ReadRequiredLine("Approve Checkbook Request");
                UserAccount? accCheckbook = userAccounts.FirstOrDefault(ua => ua.username == accCheckbookName);
                if(accCheckbook == null)
                {
                    Console.WriteLine($"Account for {accCheckbookName} does not exist.");
                    break;
                }
                ServiceRequest? pendingCheckbookRequest = serviceRequests
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
                accCheckbook.Checkbook.Add(new Checkbook(accCheckbook.Checkbook.Count + 1, "", DateTime.Now, 0, accCheckbook.routingNumber, accCheckbook.accountNumber));
                // isPending unused for now
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
        List<ServiceRequest> serviceRequests = new List<ServiceRequest>();

        AdminAccount defaultAdmin = new AdminAccount { username = "admin" };
        defaultAdmin.setPassword("Admin123!");
        adminAccounts.Add(defaultAdmin);
        Console.WriteLine("Default admin account created - username: admin / password: Admin123!");

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
                    UserAccount? uAcc = userAccounts.FirstOrDefault(u => u.username == custUsername && u.getPassword() == custPassword);
                    if(uAcc == null)
                    {
                        Console.WriteLine("Invalid Credentials");
                        break;
                    }
                    customer_options(uAcc, serviceRequests);
                    break;
                case 2:
                    string adminUsername = ReadRequiredLine("Please Enter your username");
                    string adminPassword = ReadRequiredLine("Please Enter your password");
                    AdminAccount? aAcc = adminAccounts.FirstOrDefault(a => a.username == adminUsername && a.getPassword() == adminPassword);
                    if(aAcc == null)
                    {
                        Console.WriteLine("Invalid Credentials");
                        break;
                    }
                    admin_options(aAcc, serviceRequests);
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