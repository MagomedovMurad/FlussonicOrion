using Orion;
using System;

namespace FlussonicOrion.Models
{
    public class AccessRequestResult
    {
        public AccessRequestResult(bool accessAllowed, string reason, TPersonData personData, DateTime? startDateTime, int keyId = 0)
        {
            AccessAllowed = accessAllowed;
            Reason = reason;
            PersonData = personData;
            KeyId = keyId;
            StartDateTime = startDateTime;
        }

        public bool AccessAllowed { get; set; }
        public TPersonData PersonData { get; set; }
        public string Reason { get; set; }
        public int KeyId { get; set; }
        public DateTime? StartDateTime { get; set; }
    }
}
