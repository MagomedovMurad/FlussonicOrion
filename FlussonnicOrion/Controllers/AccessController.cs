using FlussonnicOrion.Models;
using FlussonnicOrion.OrionPro;
using FlussonnicOrion.OrionPro.Enums;
using Orion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlussonnicOrion.Controllers
{
    public interface IAccessController
    {
        List<AccessRequesteResult> CheckAccess(string number, int itemId);
    }

    public class AccessController: IAccessController
    {
        private readonly IOrionCache _orionCache;

        public AccessController(IOrionCache orionCache)
        {
            _orionCache = orionCache;
        }
        
        public List<AccessRequesteResult> CheckAccess(string number, int itemId)
        {
            var keys = _orionCache.GetKeysByRegNumber(number);
            var keyAccessResults = keys.Select(x => CheckAccessByKey(x, itemId)).ToArray();

            var visits = _orionCache.GetVisitsByRegNumber(number);
            var visitAccessResults = visits.Select(x => CheckAccessByVisit(x)).ToArray();

            var allAccessResults = keyAccessResults.Concat(visitAccessResults).Where(x => x != null).ToList();

            if (allAccessResults.Count == 0)
                allAccessResults.Add(new AccessRequesteResult(false, "Не найден в системе", 0, null));

            return allAccessResults;
        }

        private AccessRequesteResult CheckAccessByVisit(TVisitData visit)
        {
            var person = _orionCache.GetPerson(visit.PersonId);
            if (person.IsInArchive)
                return null;

            var company = _orionCache.GetCompany(visit.VisitedCompanyId);
            var personData = $"{company?.Name ?? "Неизвестно"}: {person.LastName} {person.FirstName} {person.MiddleName}";

            if (person.IsInBlackList)
                return new AccessRequesteResult(false, $"В черном списке", person.Id, personData);

            else if (visit.VisitDate > DateTime.Now)
                return new AccessRequesteResult(false, $"Проход не разрешен до {visit.VisitDate}", person.Id, personData);

            else if (visit.VisitEndDateTime < DateTime.Now)
                return new AccessRequesteResult(false, $"Проход запрещен после {visit.VisitEndDateTime}", person.Id, personData);
            else
                return new AccessRequesteResult(true, null, person.Id, personData);
        }
        private AccessRequesteResult CheckAccessByKey(TKeyData key, int itemId)
        {
            var person = _orionCache.GetPerson(key.PersonId);
            if (person.IsInArchive)
                return null;

            var personData = $"{(string.IsNullOrWhiteSpace(person?.Company)? "Неизвестно": person?.Company)}: {person.LastName} {person.FirstName} {person.MiddleName}";

            if (key.IsBlocked)
                return new AccessRequesteResult(false, "Ключ заблокирован", person.Id, personData, key.Id);

            else if (key.IsInStopList)
                return new AccessRequesteResult(false, "Ключ в стоп-листе", person.Id, personData, key.Id);

            else if (key.StartDate > DateTime.Now)
                return new AccessRequesteResult(false, $"Ключ не дейстивтелен до {key.StartDate}", person.Id, personData, key.Id);

            else if (key.EndDate < DateTime.Now)
                return new AccessRequesteResult(false, $"Ключ истек {key.EndDate}", person.Id, personData, key.Id);

            else return CheckAccessLevel(key.AccessLevelId, itemId, person.Id, personData, key.Id);
        }
        private AccessRequesteResult CheckAccessLevel(int accessLevelId, int itemId, int personId, string personData, int keyId)
        {
            var accessLevel = _orionCache.GetAccessLevel(accessLevelId);
            var accessLevelItems = accessLevel.Items.Where(x => x.ItemType == ItemType.ACCESSPOINT.ToString() && x.ItemId == itemId).ToArray();
            if (accessLevelItems.Length == 0)
                return new AccessRequesteResult(false, $"Ограничено уровнем доступа", personId, personData, keyId);

            var isAccess = accessLevelItems.Select(x => CheckWindowAccess(x)).Any(x => x);
            if (!isAccess)
                return new AccessRequesteResult(false, $"Ограничено временным интервалом", personId, personData, keyId);

            return new AccessRequesteResult(true, null, personId, personData, keyId);
        }
        private bool CheckWindowAccess(TAccessLevelItem accessLevelItem)
        {
            var timeWindow = _orionCache.GetTimeWindow(accessLevelItem.TimeWindowId);
            var timeIntervals = timeWindow.TimeIntervals.Where(x => x.StartTime.TimeOfDay <= DateTime.Now.TimeOfDay
                                                     && x.EndTime.TimeOfDay >= DateTime.Now.TimeOfDay).ToArray();

            return timeIntervals.Select(x => CheckIntervalAccess(timeWindow, x)).Any(x => x);
        }
        private bool CheckIntervalAccess(TTimeWindow timeWindow, TTimeInterval timeInterval)
        {
            if (timeWindow.Calendar.Length < 31 * 12)
                return true;

            var calendarDayIndex = (DateTime.Now.Month - 1) * 31 + DateTime.Now.Day - 1;
            var calendarDayType = timeWindow.Calendar[calendarDayIndex];

            if (calendarDayType == 15)
                calendarDayType = (byte)(DateTime.Now.DayOfWeek + 6 % 7);

            return timeInterval.Days[calendarDayType];
        }
    }
}
