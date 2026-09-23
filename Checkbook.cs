using System.Security;

namespace Checkbooks
{
    public class Checkbook
    {
        public int checkbookId { get; set; }
        public string Payee { get; set; }
        public DateTime Date { get; set; }
        public long Amount { get; set; }
        public long routingNumber { get; set; }
        public long accountNumber { get; set; }
        public bool isPending;

        public Checkbook(int id, string payee, DateTime timeEntered, long amt, long rNum, long aNum )
        {
            checkbookId = id;
            Payee = "";
            Date = timeEntered;
            Amount = amt;
            routingNumber = rNum;
            accountNumber = aNum;
            isPending = false;
        }
        public bool setPending()
        {
            isPending = true;
            return isPending;
        }
    }
}