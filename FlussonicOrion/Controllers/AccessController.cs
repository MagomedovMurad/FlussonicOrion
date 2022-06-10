using FlussonicOrion.Models;
using FlussonicOrion.OrionPro.DataSources;
using FlussonicOrion.OrionPro.Enums;
using Orion;
using System;
using System.Linq;

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
                return new AccessRequestResult(false, "Не найден", null, null, 0);

            key = _orionDataSource.GetKeyByPersonIdAndComment(visit.PersonId, licensePlate);
            if (key != null)
                return CheckAccessByKey(key, itemId, direction);

            return new AccessRequestResult(false, "Не найден", null, null, 0);
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

            return new AccessRequestResult(true, string.Empty, person, key.StartDate, key.Id);
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
}
