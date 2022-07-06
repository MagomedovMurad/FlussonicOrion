using FlussonicOrion.Models;
using FlussonicOrion.OrionPro.DataSources;
using FlussonicOrion.Utils;
using Orion;
using System;
using System.Linq;

namespace FlussonicOrion.Controllers
{
    public class AccessChecker
    {
        private readonly IOrionDataSource _orionDataSource;

        public AccessChecker(IOrionDataSource dataSource)
        {
            _orionDataSource = dataSource;
        }

        public AccessRequestResult CheckAccessByPersonnelNumber(string personnelNumber, int itemId, PassageDirection direction)
        {
            TPersonData person = null;
            TKeyData key = null;
            try
            {
                person = _orionDataSource.GetPersonByTabNum(personnelNumber);
                if (person == null)
                    throw new AccessDeniedException("Нет сотрудника");
                CheckPerson(person);

                key = _orionDataSource.GetKeyByPersonId(person.Id);
                if (key == null)
                    throw new AccessDeniedException("Ключ не найден");
                CheckKey(key);

                CheckAccessLevelAndTimeWindow(key.AccessLevelId, itemId, direction);

                return new AccessRequestResult(true, string.Empty, person, key.Id);
            }
            catch (AccessDeniedException ex)
            {
                return new AccessRequestResult(false, ex.Reason, person, key?.Id ?? 0);
            }
        }
        public AccessRequestResult CheckAccessByLicensePlate(string licensePlate, int itemId, PassageDirection direction)
        {
            TPersonData person = null;
            TKeyData key = null;
            try
            {
                key = _orionDataSource.GetKeyByCode(licensePlate);
                if (key != null)
                {
                    person = _orionDataSource.GetPersonById(key.PersonId);
                    CheckPerson(person);
                    CheckKey(key);
                    CheckAccessLevelAndTimeWindow(key.AccessLevelId, itemId, direction);
                    return new AccessRequestResult(true, string.Empty, person, key.Id);
                }

                var visit = _orionDataSource.GetActualVisitByRegNumber(licensePlate);
                if (visit == null)
                    throw new AccessDeniedException("Не найден");

                person = _orionDataSource.GetPersonById(visit.PersonId);
                if(person == null)
                    throw new AccessDeniedException("Не найден");

                CheckPerson(person);

                key = _orionDataSource.GetKeyByPersonId(person.Id);
                if (key == null)
                    throw new AccessDeniedException("Ключ не найден");

                CheckKey(key);
                CheckAccessLevelAndTimeWindow(key.AccessLevelId, itemId, direction);
                return new AccessRequestResult(true, string.Empty, person, key.Id);
            }
            catch (AccessDeniedException ex)
            {
                return new AccessRequestResult(false, ex.Reason, person, key?.Id ?? 0);
            }
        }

        private void CheckPerson(TPersonData person)
        {
            if (person.IsInArchive)
                throw new AccessDeniedException("В архиве");
            else if (person.IsInBlackList)
                throw new AccessDeniedException("В черном списке");
        }
        private void CheckKey(TKeyData key)
        {
            if (key.IsBlocked)
                throw new AccessDeniedException("Заблокирован");
            else if (key.IsInStopList)
                throw new AccessDeniedException("В стоп-листе");
            else if (key.StartDate > DateTime.Now)
                throw new AccessDeniedException("Ключ не активен");
            else if (key.EndDate < DateTime.Now)
                throw new AccessDeniedException("Ключ истек");
        }
        private void CheckAccessLevelAndTimeWindow(int accessLevelId, int itemId, PassageDirection direction)
        {
            var accessLevel = _orionDataSource.GetAccessLevel(accessLevelId);
            var accessLevelItems = accessLevel.Items.Where(x => x.ItemId == itemId || x.ItemId == 0).ToArray();
            if (accessLevelItems.Length == 0)
                throw new AccessDeniedException("Уровнем доступа");

            foreach (var accessLevelItem in accessLevelItems)
                if (CheckWindowAccess(accessLevelItem, direction))
                    return;

            throw new AccessDeniedException("Временным окном");
        }
        private bool CheckWindowAccess(TAccessLevelItem accessLevelItem, PassageDirection direction)
        {
            var timeWindow = _orionDataSource.GetTimeWindow(accessLevelItem.TimeWindowId);
            var timeIntervals = timeWindow.TimeIntervals.Where(x => x.StartTime.TimeOfDay <= DateTime.Now.TimeOfDay
                                                     && x.EndTime.TimeOfDay >= DateTime.Now.TimeOfDay).ToArray();

            if (direction.Equals(PassageDirection.Entry))
                timeIntervals = timeIntervals.Where(x => x.IsEnterActivity).ToArray();

            if (direction.Equals(PassageDirection.Exit))
                timeIntervals = timeIntervals.Where(x => x.IsExitActivity).ToArray();

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
