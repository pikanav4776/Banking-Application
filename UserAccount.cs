using System.ComponentModel;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using Checkbooks;
using Request;

namespace AccountManagement
{
    public class UserAccount : Account
    {
        public long routingNumber { get; set; }
        public long accountNumber { get; set; }
        public required string AccName { get; set; }
        public double accBalance { get; set; }
        public bool IsActive { get; set; }
        public required string email { get; set; }
        public required string HomeAddress { get; set; }
        public required string SSN { get; set; } //must be ideally encrypted
        public List<Checkbook>? Checkbook { get; set; }
        public string[]? requestId { get; set; }
        public void checkAccountDetails()
        {
            Console.WriteLine($"Username:{username}");
            Console.WriteLine($"Routing Number:{routingNumber}");
            Console.WriteLine($"Account Number:{accountNumber}");
            Console.WriteLine($"Account Balance:{accBalance}"); // email
            Console.WriteLine($"Account Activity:{IsActive}");
            Console.WriteLine($"{username}'s email:{email}");
            Console.WriteLine($"{username}'s address:{HomeAddress}");
            if(Checkbook != null)
            {
                Console.WriteLine($"{username}'s Checkbook:{Checkbook} Be careful with this!");
            }
            else
            {
                Console.WriteLine($"{username} has no checkbook on file.");
            }

            if(requestId != null && requestId.Length > 0)
            {
                Console.WriteLine($"{username}'s request:{string.Join(", ", requestId)} Be careful with this!");
            }
            else
            {
                Console.WriteLine($"{username} has no pending requests.");
            }
            Console.WriteLine($"Do you want to see {username}'s SSN?");
            string userResponse = "";
            userResponse = Convert.ToString(Console.ReadLine())?.ToLower() ?? "";
            string[] acceptableAnswers = new string[] {"yes","ok","sure","true"};
            bool flag = false;
            foreach(string answer in acceptableAnswers)
            {
                if(userResponse == answer)
                {
                    flag = true; 
                }
            }
            if(flag == true)
            {
                Console.WriteLine($"{username}'s SSN:{SSN} Be careful with this!");
            }
            
        }
        public double withdraw(double amount)
        {
            accBalance -= amount;
            return accBalance;
        }
        public double deposit(double amount)
        {
            accBalance += amount;
            return accBalance;
        }
        public double changePassword(int amount)
        {
            accBalance = accBalance + amount;
            return accBalance;
        }
    }
}