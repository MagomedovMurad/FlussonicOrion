using Orion;
using System;

namespace FlussonicOrion.Models
{
    public class AccessRequestResult
    {
        public AccessRequestResult(bool accessAllowed, string reason, TPersonData personData, DateTime? startDateTime, int keyId)
        {
            AccessAllowed = accessAllowed;
            Reason = reason;
            Person = personData;
            KeyId = keyId;
            StartDateTime = startDateTime;
        }

        public bool AccessAllowed { get; set; }
        public TPersonData Person { get; set; }
        public string Reason { get; set; }
        public int KeyId { get; set; }
        public DateTime? StartDateTime { get; set; }
    }
}
