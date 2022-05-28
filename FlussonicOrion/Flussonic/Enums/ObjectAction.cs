using System.Runtime.Serialization;

namespace FlussonicOrion.Flussonic.Enums
{
    public enum ObjectAction
    {
        [EnumMember(Value = "enter")]
        Enter,
        [EnumMember(Value = "leave")]
        Leave
    }
}
