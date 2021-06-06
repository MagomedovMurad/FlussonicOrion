using System.Runtime.Serialization;

namespace FlussonnicOrion.Flussonic.Enums
{
    public enum ObjectClass
    {
        [EnumMember(Value = "vehicle")]
        Vehicle,
        [EnumMember(Value = "face")]
        Face
    }
}
