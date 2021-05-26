using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion.Flussonic.Models
{
    public enum ObjectAction
    {
        [EnumMember(Value = "enter")]
        Enter,
        [EnumMember(Value = "leave")]
        Leave
    }
}
