using System.ComponentModel;
using System.Data.Common;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using Checkbooks;
using Request;
using Transactions;

namespace AccountManagement
{
    public class UserAccount : Account
    {
        public long routingNumber { get; set; }
        public long accountNumber { get; set; }
        public required string accName { get; set; }
        public double accBalance { get; set; }
        public bool isActive { get; set; }
        public required string email { get; set; }
        public required string homeAddress { get; set; }
        public required long SSN { get; set; } //must be ideally encrypted
        public List<Checkbook>? Checkbook { get; set; }
        public List<ServiceRequest> serviceRequests { get; set; } = new();
        public List<Transaction> transactions { get; set; } = new();
        public void checkAccountDetails()
        {
            Console.WriteLine($"Username:{username}");
            Console.WriteLine($"Routing Number:{routingNumber}");
            Console.WriteLine($"Account Number:{accountNumber}");
            Console.WriteLine($"Account Balance:{accBalance}"); // email
            Console.WriteLine($"Account Activity:{isActive}");
            Console.WriteLine($"{username}'s email:{email}");
            Console.WriteLine($"{username}'s address:{homeAddress}");
            if(Checkbook != null && Checkbook.Count > 0)
            {
                Console.WriteLine($"{username} has {Checkbook.Count} checkbook(s) on file.");
            }
            else
            {
                Console.WriteLine($"{username} has no checkbook on file.");
            }

            var pendingCheckbookRequests = serviceRequests
                .Where(sr => sr.requestType == "Checkbook" && sr.accepted == null)
                .ToList();
            if(pendingCheckbookRequests.Count > 0)
            {
                Console.WriteLine($"{username} has a pending checkbook request (Request ID: {string.Join(", ", pendingCheckbookRequests.Select(sr => sr.serviceRequestId))}).");
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
                Console.WriteLine($"{username}'s SSN:{SSN:D9} Be careful with this!");
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