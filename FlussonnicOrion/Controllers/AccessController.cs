using FlussonnicOrion.Models;
using FlussonnicOrion.OrionPro;
using FlussonnicOrion.OrionPro.Enums;
using Microsoft.Extensions.Logging;
using Orion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlussonnicOrion.Controllers
{
    public interface IAccessController
    {
        List<AccessRequestResult> CheckAccess(string number, int itemId, PassageDirection direction);
    }

    public class AccessController: IAccessController
    {
        private readonly IOrionDataSource _orionDataSource;

        public AccessController(IOrionDataSource dataSource)
        {
            _orionDataSource = dataSource;
        }
        
        public List<AccessRequestResult> CheckAccess(string licensePlate, int itemId, PassageDirection direction)
        {
            var keys = _orionDataSource.GetKeysByRegNumber(licensePlate);
            var keyAccessResults = keys.Select(x => CheckAccessByKey(x, itemId, direction)).ToArray();

            var visits = _orionDataSource.GetVisitsByRegNumber(licensePlate);
            var visitAccessResults = visits.Select(x => CheckAccessByVisit(x)).ToArray();

            var allAccessResults = keyAccessResults.Concat(visitAccessResults).Where(x => x != null).ToList();

            if (allAccessResults.Count == 0)
                allAccessResults.Add(new AccessRequestResult(false, "Не найден в системе", 0, null, null));

            return allAccessResults;
        }
        private AccessRequestResult CheckAccessByVisit(TVisitData visit)
        {
            var person = _orionDataSource.GetPerson(visit.PersonId);
            if (person.IsInArchive)
                return null;

            var company = _orionDataSource.GetCompany(visit.VisitedCompanyId);
            var personData = $"{company?.Name ?? "Неизвестно"}: {person.LastName} {person.FirstName} {person.MiddleName}";

            if (person.IsInBlackList)
                return new AccessRequestResult(false, $"В черном списке", person.Id, personData, visit.VisitDate);

            else if (visit.VisitDate > DateTime.Now)
                return new AccessRequestResult(false, $"Проход не разрешен до {visit.VisitDate}", person.Id, personData, visit.VisitDate);

            else if (visit.VisitEndDateTime < DateTime.Now)
                return new AccessRequestResult(false, $"Проход запрещен после {visit.VisitEndDateTime}", person.Id, personData, visit.VisitDate);
            else
                return new AccessRequestResult(true, null, person.Id, personData, visit.VisitDate);
        }
        private AccessRequestResult CheckAccessByKey(TKeyData key, int itemId, PassageDirection direction)
        {
            var person = _orionDataSource.GetPerson(key.PersonId);
            if (person.IsInArchive)
                return null;

            var personData = $"{(string.IsNullOrWhiteSpace(person?.Company)? "Неизвестно": person?.Company)}: {person.LastName} {person.FirstName} {person.MiddleName}";

            if (key.IsBlocked)
                return new AccessRequestResult(false, "Ключ заблокирован", person.Id, personData, key.StartDate, key.Id);

            else if (key.IsInStopList)
                return new AccessRequestResult(false, "Ключ в стоп-листе", person.Id, personData, key.StartDate, key.Id);

            else if (key.StartDate > DateTime.Now)
                return new AccessRequestResult(false, $"Ключ не дейстивтелен до {key.StartDate}", person.Id, personData, key.StartDate, key.Id);

            else if (key.EndDate < DateTime.Now)
                return new AccessRequestResult(false, $"Ключ истек {key.EndDate}", person.Id, personData, key.StartDate, key.Id);

            else return CheckAccessLevel(key.AccessLevelId, itemId, person.Id, personData, key, direction);
        }
        private AccessRequestResult CheckAccessLevel(int accessLevelId, int itemId, int personId, string personData, TKeyData key, PassageDirection direction)
        {
            var accessLevel = _orionDataSource.GetAccessLevel(accessLevelId);
            var accessLevelItems = accessLevel.Items
                                              .Where(x => x.ItemType == ItemType.ACCESSPOINT.ToString() 
                                                          && (x.ItemId == itemId || x.ItemId == 0))
                                              .ToArray();
            if (accessLevelItems.Length == 0)
                return new AccessRequestResult(false, $"Ограничено уровнем доступа", personId, personData, key.StartDate, key.Id);

            var isAccess = accessLevelItems.Select(x => CheckWindowAccess(x, direction)).Any(x => x);
            if (!isAccess)
                return new AccessRequestResult(false, $"Ограничено временным интервалом", personId, personData, key.StartDate, key.Id);

            return new AccessRequestResult(true, null, personId, personData, key.StartDate, key.Id);
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
