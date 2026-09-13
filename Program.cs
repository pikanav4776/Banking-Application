using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using AccountManagement;
using System.IO;
using System.Net.Mime;
using System.Text.RegularExpressions;

public class Program()
{
    
    #region helpermethods
        public static void transfer(Account acc, List<Account> accs)
        {

            //ask for account 2
            string transferAccountName = Console.ReadLine();

            int transferRoutingNumber = Convert.ToInt32(Console.ReadLine());

            int accountNumber = Convert.ToInt32(Console.ReadLine());

            var transferAcc = from account in accs where acc.AccName == transferAccountName 
            && acc.routingNumber == transferRoutingNumber 
            && acc.accountNumber == accountNumber select acc;

            Account destAcc = null;
            foreach(var item in transferAcc)
            {
                destAcc = item;
            }
            Console.WriteLine("How much do you want to withdraw");
            double withdrawAmount = Convert.ToInt32(Console.ReadLine());
            double transferMoney = acc.withdraw(withdrawAmount);

            double newAccount = destAcc.deposit(transferMoney);


        }
    #endregion
    public static void customer_options(Account acc, List<Account> accs)
    {
        for(int i = 0; i < 3; i++) // user has maximum of 3 tries
        {
            int option = Convert.ToInt32(Console.ReadLine());
            // if information is correct:
            switch (option)
            {
                case 1:
                    acc.checkAccountDetails();
                    break;
                case 2:
                    Console.WriteLine("How much do you want to withdraw?");
                    double withdrawAmount = Convert.ToDouble(Console.ReadLine());
                    acc.withdraw(withdrawAmount);
                    break;
                case 3:
                    Console.WriteLine("How much do you want to deposit?");
                    double depositAmount = Convert.ToDouble(Console.ReadLine());
                    acc.withdraw(depositAmount);
                    break;
                case 4:
                    transfer(acc, accs);
                    break;
                case 5:
                    //last 5 transactions will be included in a database and a Log file[in excel].
                    /*var transaction_record = (from x in Transactions
                       orderby x.Id descending
                       select x).Take(5).ToList();
                    foreach(var item in transaction_record)
                    {
                        string content = item.id + " " + item.Name + " " + item.accountNumber + " " + item.DateTime + " " + item.AccName + " " + item.Description;
                        string path = "C:\\Revature\\Training\\25082026---.net-fullstack-AI-enabled-Developers\\P0 Banking Application\\bankingapplication";
                        Console.WriteLine(content);
                        File.WriteAllText(path, content);
                    }*/
                    break;
                case 6://

                    break;
                case 7:
                    Console.Write("Username:");
                    string username = Console.ReadLine();
                    string accUsername = (from ac in accs where ac.username == username select ac.username).First();
                    string newPassword = "";
                    if(username == accUsername)
                    {
                        Console.WriteLine("Your new password:");
                        Console.WriteLine("     Must be between 9-15 characters:");
                        Console.WriteLine("     Must contain uppercase characters");
                        Console.WriteLine("     Must contain lowercase characters");
                        Console.WriteLine("     Must contain at least 1 number");
                        Console.WriteLine("     Must contain at least 1 special character; Examples:.!@#");
                        newPassword = Console.ReadLine();

                        string pattern = @"\w[!@#$%^&*():]";
                        MatchCollection matches;

                        Regex defaultRegex = new Regex(pattern);

                        matches = defaultRegex.Matches(newPassword);
                        if(matches != null)
                        {
                            acc.setPassword(newPassword);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Sorry, your username is incorrect;");
                    }
                    break;
                default:
                    break;
            }

            // else
            

        }
    }
    public static void admin_options()
    {
        
    }
    static void Main(string[] args)
    {
        List<Account> accs = new List<Account>();
        int option = 0;
        while (option != 3)
        {
            Console.WriteLine("Welcome to this banking application!");
            Console.WriteLine("1.Customer");
            Console.WriteLine("2.Admin");
            Console.WriteLine("3.Exit");
            Account acc = null;
            switch (option)
            {
                case 1:
                    string username = "";
                    string password = "";
                    for(int i = 0; i < 3; i++)
                    {
                        Console.WriteLine("What is your username");
                        username = Console.ReadLine();

                        Console.WriteLine("What is your password");
                        password = Console.ReadLine();

                        var accountCheck = from a in accs where a.username == username && a.password == password select a; 
                        if(accountCheck == null)
                        {
                            Console.WriteLine("The user you entered does not exist");
                        }
                        else
                        {
                            foreach(var item in accountCheck)
                            {
                                acc = item;
                            }
                            break;
                        }
                    }
                    // verify user name and password
                    customer_options(acc, accs);
                    break;
                case 2:
                    admin_options();
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