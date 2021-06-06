using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion.Models
{
    public class AccessRequesteResult
    {
        public AccessRequesteResult(bool accessAllowed, string reason, int personId, string personData, int keyId = 0)
        {
            AccessAllowed = accessAllowed;
            Reason = reason;
            PersonId = personId;
            PersonData = personData;
            KeyId = keyId;

        }

        public bool AccessAllowed { get; set; }
        public int PersonId { get; set; }
        public string PersonData { get; set; }
        public string Reason { get; set; }

        public int KeyId { get; set; }
    }
}
