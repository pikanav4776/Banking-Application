using System.ComponentModel;

namespace AccountManagement
{
    public class Account
    {
        public string username { get; set; }
        protected string _password;
        public long routingNumber { get; set; }
        public long accountNumber { get; set; }
        public string AccName { get; set; }
        public double accBalance { get; set; }
        public bool IsActive { get; set; }
        public string email { get; set; }
        public string HomeAddress { get; set; }
        public string SSN { get; set; }
        
        
        public void checkAccountDetails()
        {
            Console.WriteLine($"Username:{username}");
            Console.WriteLine($"Password:{_password}");
            Console.WriteLine($"Routing Number:{routingNumber}");
            Console.WriteLine($"Account Number:{accountNumber}");
            Console.WriteLine($"Account Balance:{accBalance}"); // email
            Console.WriteLine($"Account Activity:{IsActive}");
            Console.WriteLine($"{username}'s email:{email}");
            Console.WriteLine($"{username}'s address:{HomeAddress}");
            Console.WriteLine($"{username}'s SSN:{SSN} Be careful with this!");
        }

        public double withdraw(double amount)
        {
            accBalance = accBalance - amount;
            return accBalance;
        }

        public double deposit(double amount)
        {
            accBalance = accBalance + amount;
            return accBalance;
        }

        public double changePassword(int amount)
        {
            accBalance = accBalance + amount;
            return accBalance;
        }
        public string getPassword()
        {
            string Password = _password;
            return Password;
        }
        public string setPassword(string newPassword)
        {
            _password = newPassword;
            return _password;
        }
    }
}