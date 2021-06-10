using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion.Models
{
    public class AccessRequestResult
    {
        public AccessRequestResult(bool accessAllowed, string reason, int personId, string personData, DateTime? startDateTime, int keyId = 0)
        {
            AccessAllowed = accessAllowed;
            Reason = reason;
            PersonId = personId;
            PersonData = personData;
            KeyId = keyId;
            StartDateTime = startDateTime;
        }

        public bool AccessAllowed { get; set; }
        public int PersonId { get; set; }
        public string PersonData { get; set; }
        public string Reason { get; set; }
        public int KeyId { get; set; }
        public DateTime? StartDateTime { get; set; }
    }
}
