using System;

namespace Request
{
    public class ServiceRequest
    {
        public int serviceRequestId;
        public long accountNumber;
        public string requestor;
        public string requestType;
        public string description;
        public DateTime dateSent;
        public DateTime? dateResponded;
        public bool? accepted; // null = pending, true = accepted, false = rejected

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
