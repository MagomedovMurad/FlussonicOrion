using System.Linq;

namespace FlussonicOrion.Utils
{
    public class ShortStringHelper
    {
        public static string CreateSring(string licensePlate, string accessResult, string company, string fullName, string reason)
        {
            var licensePlateItem = new LineItem(LineItemType.LicensePlate, 1, licensePlate, 10);
            var accessResultItem = new LineItem(LineItemType.AccessResult, 2, accessResult, 6);
            var companyItem = new LineItem(LineItemType.Company, 5, company, 15);
            var fullNameItem = new LineItem(LineItemType.FullName, 4, fullName, 15);
            var reasonItem = new LineItem(LineItemType.Reason, 3, reason, 15);

            return CreateSring(licensePlateItem, accessResultItem, companyItem, fullNameItem, reasonItem);
        }
        public static string CreateSring(params LineItem[] items)
        {
            var orderedItems = items.OrderBy(x => x.Priority).ToList();

            foreach (var testData in orderedItems)
                testData.TrimmedValue = Substring(testData.Value, testData.MaxLength);

            var result = string.Join(" ", orderedItems.Select(x => x.TrimmedValue).Where(x => !string.IsNullOrWhiteSpace(x)));

            int freeSymbolsCount = 65 - result.Length;

            foreach (var testData in orderedItems)
            {
                if (freeSymbolsCount == 0)
                    return string.Join(" ", orderedItems.Select(x => x.TrimmedValue).Where(x => !string.IsNullOrWhiteSpace(x)));

                if (testData.Value.Length > testData.MaxLength)
                {
                    var substring = Substring(testData.Value, testData.TrimmedValue.Length + freeSymbolsCount);
                    freeSymbolsCount -= (substring.Length - testData.TrimmedValue.Length);
                    testData.TrimmedValue = substring;
                }
            }
            return string.Join(" ", orderedItems.Select(x => x.TrimmedValue).Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        private static string Substring(string value, int maxCount)
        {
            if (value.Length <= maxCount)
                return value;

            else
                return value.Substring(0, maxCount);
        }
    }
}
