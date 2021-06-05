using FlussonnicOrion.Models;
using FlussonnicOrion.OrionPro;
using FlussonnicOrion.OrionPro.Enums;
using Orion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion.Controllers
{
    public class AccessController
    {
        private readonly OrionCache _orionCache;

        public AccessController(OrionCache orionCache)
        {
            _orionCache = orionCache;
        }

        private IEnumerable<AccessRequesteResult> CheckAccess(string number, int itemId)
        {
            var keys = _orionCache.GetKeysByRegNumber(number);
            var keyAccessResults = keys.Select(x => CheckAccessByKey(x, itemId));

            var visits = _orionCache.GetVisitsByRegNumber(number);
            var visitAccessResults = visits.Select(x => CheckAccessByVisit(x));

            return keyAccessResults.Concat(visitAccessResults).Where(x => x != null);
        }

        private AccessRequesteResult CheckAccessByVisit(TVisitData visit)
        {
            var person = _orionCache.GetPerson(visit.PersonId);
            if (person.IsInArchive)
                return null;

            var company = _orionCache.GetCompany(visit.VisitedCompanyId);
            var personData = $"{company}: {person.LastName} {person.FirstName} {person.MiddleName}";

            if (person.IsInBlackList)
                return new AccessRequesteResult(false, $"Находится в черном списке по причине {person.BlackListComment}", personData);

            else if (visit.VisitDate > DateTime.Now)
                return new AccessRequesteResult(false, $"Проход не разрешен до {visit.VisitDate}", personData);

            else if (visit.VisitEndDateTime < DateTime.Now)
                return new AccessRequesteResult(false, $"Проход запрещен после {visit.VisitEndDateTime}", personData);
            else
                return new AccessRequesteResult(true, null, personData);
        }
        private AccessRequesteResult CheckAccessByKey(TKeyData key, int itemId)
        {
            var person = _orionCache.GetPerson(key.PersonId);
            if (person.IsInArchive)
                return null;

            var personData = $"{person.Company}: {person.LastName} {person.FirstName} {person.MiddleName}";

            if (key.IsBlocked)
                return new AccessRequesteResult(false, "Ключ заблокирован", personData);

            else if (key.IsInStopList)
                return new AccessRequesteResult(false, "Ключ в стоп-листе", personData);

            else if (key.StartDate > DateTime.Now)
                return new AccessRequesteResult(false, $"Ключ не дейстивтелен до {key.StartDate}", personData);

            else if (key.EndDate < DateTime.Now)
                return new AccessRequesteResult(false, $"Срок действия ключа истек {key.EndDate}", personData);

            else return CheckAccessLevel(key.AccessLevelId, itemId, personData);
        }
        private AccessRequesteResult CheckAccessLevel(int accessLevelId, int itemId, string personData)
        {
            var accessLevel = _orionCache.GetAccessLevel(accessLevelId);
            var accessLevelItems = accessLevel.Items.Where(x => x.ItemType == ItemType.ACCESSPOINT.ToString() && x.ItemId == itemId).ToArray();
            if (accessLevelItems.Length == 0)
                return new AccessRequesteResult(false, $"Доступ к шлагбауму {itemId} ограничен уровнем доступа ключа", personData);

            var isAccess = accessLevelItems.Select(x => CheckWindowAccess(x)).Any(x => x);
            if (!isAccess)
                return new AccessRequesteResult(false, $"Доступ ограничен временным интервалом уровня доступа ключа", personData);

            return new AccessRequesteResult(true, null, null);
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
            var calendarDayIndex = (DateTime.Now.Month - 1) * 31 + DateTime.Now.Day - 1;
            var calendarDayType = timeWindow.Calendar[calendarDayIndex];

            if (calendarDayType == 15)
                calendarDayType = (byte)(DateTime.Now.DayOfWeek - 1);

            return timeInterval.Days[calendarDayType];
        }
    }
}
