using System;

namespace Request
{
    public class ServiceRequest
    {
        public int serviceRequestId { get; set; }
        public long accountNumber { get; set; }
        public string requestor { get; set; } = string.Empty;
        public string requestType { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public DateTime dateSent { get; set; }
        public DateTime? dateResponded { get; set; }
        public bool? accepted { get; set; } // null = pending, true = accepted, false = rejected

        private ServiceRequest() { }

        public ServiceRequest(int pServiceRequestId, long pAccountNumber, string pRequestor, string pRequestType, string pDescription)
        {
            serviceRequestId = pServiceRequestId;
            accountNumber = pAccountNumber;
            requestor = pRequestor;
            requestType = pRequestType;
            description = pDescription;
            dateSent = DateTime.Now;
            dateResponded = null;
            accepted = null;
        }

        public void approve()
        {
            accepted = true;
            dateResponded = DateTime.Now;
        }

        public void reject()
        {
            accepted = false;
            dateResponded = DateTime.Now;
        }
    }
}
