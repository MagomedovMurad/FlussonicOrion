using FlussonicOrion.Controllers;

namespace FlussonicOrion.Utils
{
    public class LineItem
    {
        public LineItem(LineItemType type, int priority, string value, int maxLength)
        {
            Type = type;
            Priority = priority;
            Value = value;
            MaxLength = maxLength;
        }

        public LineItemType Type { get; set; }
        public int Priority { get; set; }
        public string Value { get; set; }
        public int MaxLength { get; set; }
        public string TrimmedValue { get; set; }
    }
}
