using System.Runtime.Serialization;

namespace FlussonnicOrion.Flussonic.Enums
{
    public enum ObjectAction
    {
        [EnumMember(Value = "enter")]
        Enter,
        [EnumMember(Value = "leave")]
        Leave
    }
}
