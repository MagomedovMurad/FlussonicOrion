using FlussonicOrion.Models;
using FlussonicOrion.OrionPro.DataSources;
using FlussonicOrion.OrionPro.Enums;
using Orion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlussonicOrion.Controllers
{
    public interface IAccessController
    {
        AccessRequestResult CheckAccess(string number, int itemId, PassageDirection direction);
    }

    public class AccessController: IAccessController
    {
        private readonly IOrionDataSource _orionDataSource;

        public AccessController(IOrionDataSource dataSource)
        {
            _orionDataSource = dataSource;
        }
        
        public AccessRequestResult CheckAccess(string licensePlate, int itemId, PassageDirection direction)
        {
            var key = _orionDataSource.GetKeyByCode(licensePlate);
            if (key != null)
                return CheckAccessByKey(key, itemId, direction);

            var visit = _orionDataSource.GetActualVisitByRegNumber(licensePlate);
            if(visit == null)
                return new AccessRequestResult(false, "Не найден", null, null);

            key = _orionDataSource.GetKeyByPersonIdAndComment(visit.PersonId, licensePlate);
            if (key != null)
                return CheckAccessByKey(key, itemId, direction);

            return new AccessRequestResult(false, "Не найден", null, null);
        }
       
        private AccessRequestResult CheckAccessByKey(TKeyData key, int itemId, PassageDirection direction)
        {
            var person = _orionDataSource.GetPerson(key.PersonId);

            if(person.IsInArchive)
                return new AccessRequestResult(false, "В архиве", person, key.StartDate, key.Id);
            else if (person.IsInBlackList)
                return new AccessRequestResult(false, $"В черном списке", person, key.StartDate, key.Id);
            else if (key.IsBlocked)
                return new AccessRequestResult(false, "Заблокирован", person, key.StartDate, key.Id);
            else if (key.IsInStopList)
                return new AccessRequestResult(false, "В стоп-листе", person, key.StartDate, key.Id);
            else if (key.StartDate > DateTime.Now)
                return new AccessRequestResult(false, $"Ключ не активен", person, key.StartDate, key.Id);
            else if (key.EndDate < DateTime.Now)
                return new AccessRequestResult(false, $"Ключ истек", person, key.StartDate, key.Id);
            else 
                return CheckAccessLevel(key.AccessLevelId, itemId, person, key, direction);
        }
        private AccessRequestResult CheckAccessLevel(int accessLevelId, int itemId, TPersonData person, TKeyData key, PassageDirection direction)
        {
            var accessLevel = _orionDataSource.GetAccessLevel(accessLevelId);
            var accessLevelItems = accessLevel.Items
                                              .Where(x => x.ItemType == ItemType.ACCESSPOINT.ToString() 
                                                          && (x.ItemId == itemId || x.ItemId == 0))
                                              .ToArray();
            if (accessLevelItems.Length == 0)
                return new AccessRequestResult(false, $"Уровнем доступа", person, key.StartDate, key.Id);

            var isAccess = accessLevelItems.Select(x => CheckWindowAccess(x, direction)).Any(x => x);
            if (!isAccess)
                return new AccessRequestResult(false, $"Временным окном", person, key.StartDate, key.Id);

            return new AccessRequestResult(true, null, person, key.StartDate, key.Id);
        }
        private bool CheckWindowAccess(TAccessLevelItem accessLevelItem, PassageDirection direction)
        {
            var timeWindow = _orionDataSource.GetTimeWindow(accessLevelItem.TimeWindowId);
            var timeIntervals = timeWindow.TimeIntervals.Where(x => x.StartTime.TimeOfDay <= DateTime.Now.TimeOfDay
                                                     && x.EndTime.TimeOfDay >= DateTime.Now.TimeOfDay).ToArray();

            if (direction.Equals(PassageDirection.Entry))
                timeIntervals = timeIntervals.Where(x => x.IsEnterActivity).ToArray();

            if (direction.Equals(PassageDirection.Exit))
                timeIntervals = timeIntervals.Where(x => x.IsEnterActivity).ToArray();

            return timeIntervals.Select(x => CheckIntervalAccess(timeWindow, x)).Any(x => x);
        }
        private bool CheckIntervalAccess(TTimeWindow timeWindow, TTimeInterval timeInterval)
        {
            if (timeWindow.Calendar.Length < 31 * 12)
                return true;

            var calendarDayIndex = (DateTime.Now.Month - 1) * 31 + DateTime.Now.Day - 1;
            var calendarDayType = timeWindow.Calendar[calendarDayIndex];

            if (calendarDayType == 15)
                calendarDayType = (byte)((int)(DateTime.Now.DayOfWeek + 6) % 7);

            return timeInterval.Days[calendarDayType];
        }
    }

    public class ShortStringHelper
    {
        public string CreateSring(string licensePlate, string accessResult, string company, string fullName, string reason)
        {
            var licensePlateItem = new LineItem(LineItemType.LicensePlate, 1, licensePlate, 10);
            var accessResultItem = new LineItem(LineItemType.AccessResult, 2, accessResult, 6);
            var companyItem = new LineItem(LineItemType.Company, 5, company, 15);
            var fullNameItem = new LineItem(LineItemType.FullName, 4, fullName, 15);
            var reasonItem = new LineItem(LineItemType.Reason, 3, reason, 15);

            return CreateSring(licensePlateItem, accessResultItem, companyItem, fullNameItem, reasonItem);
        }

        public string CreateSring(params LineItem[] items)
        {
            var orderedItems = items.OrderBy(x => x.Priority).ToList();

            foreach (var testData in orderedItems)
                testData.TrimmedValue = Substring(testData.Value, testData.MaxLength);

            var result = string.Join(" ", orderedItems.Select(x => x.TrimmedValue));

            int freeSymbolsCount = 65 - result.Length;

            foreach (var testData in orderedItems)
            {
                if (freeSymbolsCount == 0)
                    return string.Join(" ", orderedItems.Select(x => x.TrimmedValue));

                if (testData.Value.Length > testData.MaxLength)
                {
                    var substring = Substring(testData.Value, testData.TrimmedValue.Length + freeSymbolsCount);
                    freeSymbolsCount -= (substring.Length - testData.TrimmedValue.Length);
                    testData.TrimmedValue = substring;
                }
            }
            return string.Join(" ", orderedItems.Select(x => x.TrimmedValue));
        }

        private string Substring(string value, int maxCount)
        {
            if (value.Length <= maxCount)
                return value;

            else
                return value.Substring(0, maxCount);
        }
    }

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

    public enum LineItemType
    {
        LicensePlate,
        AccessResult,
        Company,
        FullName,
        Reason
    }
}
