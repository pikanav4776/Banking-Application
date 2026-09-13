namespace Checkbooks
{
    public class Checkbook
    {
        public int id { get; set; }
        public string Payee { get; set; }
        public long Date { get; set; }
        public long Amount { get; set; }
        public string Memo { get; set; }
        public string Signature { get; set; }
        public long routingNumber { get; set; }
        public long accountNumber { get; set; }

    }
}