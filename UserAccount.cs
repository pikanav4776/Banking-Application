using System.Text;
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
        public decimal accBalance { get; set; }
        public bool isActive { get; set; }
        public required string email { get; set; }
        public required string homeAddress { get; set; }
        public string ssnHash { get; private set; } = string.Empty; // salted hash only; the SSN itself is never stored
        public List<Checkbook> Checkbook { get; set; } = new();
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
            if(Checkbook.Count > 0)
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
            // the SSN is stored as a hash, so it can't be displayed, only checked against what is typed in
            Console.WriteLine($"To check an SSN against the one on file for {username}, enter it now (or press Enter to skip):");
            string enteredSsn = Program.censorInput(new StringBuilder());
            Console.WriteLine();
            if(enteredSsn.Length > 0)
            {
                Console.WriteLine(verifySsn(enteredSsn) ? "That SSN matches the one on file." : "That SSN does not match the one on file.");
            }
        }

        public void setSsn(string ssn)
        {
            ssnHash = Hash(ssn);
        }

        public bool verifySsn(string ssn)
        {
            return Verify(ssn, ssnHash);
        }

        public decimal withdraw(decimal amount)
        {
            accBalance -= amount;
            return accBalance;
        }
        public decimal deposit(decimal amount)
        {
            accBalance += amount;
            return accBalance;
        }
    }
}