using Orion;
using System;

namespace FlussonicOrion.Models
{
    public class AccessRequestResult
    {
        public AccessRequestResult(bool accessAllowed, string reason, TPersonData personData, int keyId)
        {
            AccessAllowed = accessAllowed;
            Reason = reason;
            Person = personData;
            KeyId = keyId;
        }

        public bool AccessAllowed { get; set; }
        public TPersonData Person { get; set; }
        public string Reason { get; set; }
        public int KeyId { get; set; }
    }
}
