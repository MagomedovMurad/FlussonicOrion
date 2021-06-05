using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion.Models
{
    public class AccessRequesteResult
    {
        public AccessRequesteResult(bool accessAllowed, string reason, string personData)
        {
            AccessAllowed = accessAllowed;
            Reason = reason;
            PersonData = personData;
        }

        public bool AccessAllowed { get; set; }

        public string PersonData { get; set; }

        public string Reason { get; set; }
    }
}
