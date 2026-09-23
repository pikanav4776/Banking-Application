namespace Transactions
{
    public class Transaction
    {
        public required int id { get; set; }
        public required string Name { get; set; }
        public required long accountNumber { get; set; }
        public required long DateTime { get; set; }
        public required string AccName { get; set; }
        public required string Description { get; set; }
    }
}