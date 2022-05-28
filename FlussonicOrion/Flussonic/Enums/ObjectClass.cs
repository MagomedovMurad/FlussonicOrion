using System.Runtime.Serialization;

namespace FlussonicOrion.Flussonic.Enums
{
    public enum ObjectClass
    {
        [EnumMember(Value = "vehicle")]
        Vehicle,
        [EnumMember(Value = "face")]
        Face
    }
}
