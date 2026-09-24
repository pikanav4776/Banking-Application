namespace Transactions
{
    public class Transaction
    {
        public int id { get; set; }
        public string accName { get; set; } = string.Empty;
        public long accountNumber { get; set; }
        public System.DateTime DateTime { get; set; }
        public string description { get; set; } = string.Empty;

        private Transaction() { }

        public Transaction(string paramAccName, long paramAccNumber, string paramDescription)
        {
            accName = paramAccName;
            accountNumber = paramAccNumber;
            description = paramDescription;
            DateTime = System.DateTime.Now;
        }
    }
}
