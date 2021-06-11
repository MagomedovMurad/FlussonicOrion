using FlussonnicOrion.OrionPro.Enums;
using Microsoft.Extensions.Logging;
using Orion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FlussonnicOrion.OrionPro
{
    public interface IOrionDataSource
    {
        void Initialize(int employeeInterval, int visitorsInterval);
        void Dispose();

        IEnumerable<TVisitData> GetVisitsByRegNumber(string regNumber);
        IEnumerable<TKeyData> GetKeysByRegNumber(string regNumber);
        TPersonData GetPerson(int id);
        TAccessLevel GetAccessLevel(int id);
        TTimeWindow GetTimeWindow(int id);
        TCompany GetCompany(int id);
    }

    public class OrionCacheDataSource: IOrionDataSource
    {
        private readonly ILogger<IOrionDataSource> _logger;
        private readonly IOrionClient _orionClient;

        #region Timers
        private System.Timers.Timer _personsTimer;
        private System.Timers.Timer _visitorsTimer;
        private System.Timers.Timer _timeWindowsTimer;
        private System.Timers.Timer _keysTimer;
        private System.Timers.Timer _accessLevelsTimer;
        private System.Timers.Timer _companiesTimer;
        #endregion

        #region Items list
        private IEnumerable<TPersonData> _persons;
        private IEnumerable<TVisitData> _visitors;
        private IEnumerable<TTimeWindow> _timeWindows;
        private IEnumerable<TKeyData> _keys;
        private IEnumerable<TAccessLevel> _accessLevels;
        private IEnumerable<TCompany> _companies;
        #endregion

        #region Locks
        private ReaderWriterLockSlim _personsLock = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim _visitorsLock = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim _timeWindowsLock = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim _keysLock = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim _accessLevelsLock = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim _companiesLock = new ReaderWriterLockSlim();
        #endregion

        private Dictionary<string, string> _cyrillicToLatin = new Dictionary<string, string>()
        {
            { "А", "A"},
            { "В", "B" },
            { "Е", "E"},
            { "К", "K"},
            { "М", "M"},
            { "Н", "H"},
            { "О", "O"},
            { "Р", "P"},
            { "С", "C"},
            { "Т", "T"},
            { "У", "Y"},
            { "Х", "X"}
        };
        public OrionCacheDataSource(IOrionClient orionClient, ILogger<IOrionDataSource> logger)
        {
            _orionClient = orionClient;
            _logger = logger;
        }

        public void Initialize(int employeeInterval, int visitorsInterval)
        {
            _logger.LogInformation("Запуск инициализации кэша данных Орион");
            _personsTimer = CreateTimer(employeeInterval, PersonUpdater);
            _keysTimer = CreateTimer(employeeInterval, KeysUpdater);
            _timeWindowsTimer = CreateTimer(employeeInterval, TimeWindowsUpdater);
            _accessLevelsTimer = CreateTimer(employeeInterval, AccessLevelsUpdater);

            _visitorsTimer = CreateTimer(visitorsInterval, VisitorsUpdater);
            _companiesTimer = CreateTimer(visitorsInterval, CompaniesUpdater);

            PersonUpdater(this, null);
            VisitorsUpdater(this, null);
            TimeWindowsUpdater(this, null);
            KeysUpdater(this, null);
            AccessLevelsUpdater(this, null);
            CompaniesUpdater(this, null);
            _logger.LogInformation("Кэш данных Орион инициализирован");
        }
        public void Dispose()
        {
            _personsTimer.Dispose();
            _visitorsTimer.Dispose();
            _timeWindowsTimer.Dispose();
            _keysTimer.Dispose();
            _accessLevelsTimer.Dispose();
            _companiesTimer.Dispose();
        }

        public IEnumerable<TVisitData> GetVisitsByRegNumber(string regNumber)
        {
            return ReadList(() => _visitors.Where(x => x.CarNumber.Equals(regNumber)), _visitorsLock).ToArray();
        }
        public IEnumerable<TKeyData> GetKeysByRegNumber(string regNumber)
        {
            return ReadList(() => _keys.Where(x => x.Code.Equals(regNumber)), _keysLock).ToArray();
        }
        public TPersonData GetPerson(int id)
        {
            return ReadList(() => _persons.FirstOrDefault(x => x.Id.Equals(id)), _personsLock);
        }
        public TAccessLevel GetAccessLevel(int id)
        {
            return _accessLevels.FirstOrDefault(x => x.Id == id);
        }
        public TTimeWindow GetTimeWindow(int id)
        {
            return _timeWindows.FirstOrDefault(x => x.Id == id);
        }
        public TCompany GetCompany(int id)
        {
            return _companies.FirstOrDefault(x => x.Id == id);
        }


        #region Sync
        private void PersonUpdater(object sender, ElapsedEventArgs e)
        {
            LoadPersons().ContinueWith(t =>
            {
                if (t.Result != null)
                    UpdateList(t.Result, ref _persons, _personsLock);

                _personsTimer.Start();
            });
        }
        private void VisitorsUpdater(object sender, ElapsedEventArgs e)
        {
            LoadVisitors().ContinueWith(t =>
            {
                if (t.Result != null)
                    UpdateList(t.Result, ref _visitors, _visitorsLock);

                _visitorsTimer.Start();
            });
        }
        private void TimeWindowsUpdater(object sender, ElapsedEventArgs e)
        {
            _orionClient.GetTimeWindows().ContinueWith(t =>
            {
                if (t.Result != null)
                {
                    UpdateList(t.Result, ref _timeWindows, _timeWindowsLock);
                }

                _timeWindowsTimer.Start();
            });
        }
        private void KeysUpdater(object sender, ElapsedEventArgs e)
        {
            LoadKeys().ContinueWith(t =>
            {
                if (t.Result != null)
                    UpdateList(t.Result, ref _keys, _keysLock);

                _keysTimer.Start();
            });
        }
        private void AccessLevelsUpdater(object sender, ElapsedEventArgs e)
        {
            LoadAccessLevels().ContinueWith(t =>
            {
                if (t.Result != null)
                {
                    UpdateList(t.Result, ref _accessLevels, _accessLevelsLock);
                }

                _accessLevelsTimer.Start();
            });
        }
        private void CompaniesUpdater(object sender, ElapsedEventArgs e)
        {
            _orionClient.GetCompanies(false, false).ContinueWith(t =>
            {
                if (t.Result != null)
                {
                    UpdateList(t.Result, ref _companies, _companiesLock);
                }

                _companiesTimer.Start();
            });
        }

        private async Task<IEnumerable<TPersonData>> LoadPersons()
        {
            var allPersons = new List<TPersonData>();
            var personsCount = await _orionClient.GetPersonsCount();

            for (int i = 0; i < personsCount; i += 100)
            {
                var persons = await _orionClient.GetPersons(true, i, 100, null, false, false);
                if (persons == null)
                {
                    _logger.LogError("Ошибка при получении списка TPerson");
                    return null;
                }
                allPersons.AddRange(persons);
                _logger.LogInformation($"Получение списка TPerson: {allPersons.Count} из {personsCount}");
            }

            return allPersons;
        }
        private async Task<IEnumerable<TVisitData>> LoadVisitors()
        {
            var visitors = await _orionClient.GetVisits();
            if (visitors == null)
            {
                _logger.LogError("Ошибка при получении списка TVisit");
                return null;
            }
            visitors.ToList().ForEach(x => x.CarNumber = ReplaceCirilicToLatin(x.CarNumber));
            _logger.LogInformation($"Получение списка TVisit: {visitors.Length} из {visitors.Length}");
            return visitors;
        }
        private async Task<IEnumerable<TKeyData>> LoadKeys()
        {
            var allKeys = new List<TKeyData>();
            var keysCount = await _orionClient.GetKeysCount();

            for (int i = 0; i < keysCount; i += 100)
            {
                var keys = await _orionClient.GetKeys(i, 100);
                if (keys == null)
                {
                    _logger.LogError("Ошибка при получении списка TKeyData");
                    return null;
                }
                allKeys.AddRange(keys);
                _logger.LogInformation($"Получение списка TKeyData: {allKeys.Count} из {keysCount}");
            }

            var carNumbers = allKeys.Where(x => x.CodeType == (int)CodeType.CarNumber).ToList();
            carNumbers.ForEach(x => x.Code = ReplaceCirilicToLatin(x.Code));
            return carNumbers;
        }
        private async Task<IEnumerable<TAccessLevel>> LoadAccessLevels()
        {
            var allAccessLevels = new List<TAccessLevel>();
            var accessLevelsCount = await _orionClient.GetAccessLevelsCount();

            for (int i = 0; i < accessLevelsCount; i += 100)
            {
                var accessLevels = await _orionClient.GetAccessLevels(i, 100);
                if (accessLevels == null)
                {
                    _logger.LogError("Ошибка при получении списка TAccessLevel");
                    return null;
                }
                allAccessLevels.AddRange(accessLevels);
                _logger.LogInformation($"Получение списка TAccessLevel: {allAccessLevels.Count} из {accessLevelsCount}");
            }

            return allAccessLevels;
        }

        private System.Timers.Timer CreateTimer(int interval, ElapsedEventHandler handler)
        {
            if (interval == 0)
                interval = 1;

            var timer = new System.Timers.Timer(interval * 1000);
            timer.Elapsed += handler;
            timer.AutoReset = false;
            return timer;
        }

        private string ReplaceCirilicToLatin(string data)
        {
            var upperCaseText = data.Replace(" ","").ToUpper();

            foreach (var symbol in _cyrillicToLatin)
                upperCaseText = upperCaseText.Replace(symbol.Key, symbol.Value);

            return upperCaseText;
        }

        private void UpdateList<T>(IEnumerable<T> actualItems, ref IEnumerable<T> list, ReaderWriterLockSlim lockSlim)
        {
            lockSlim.EnterWriteLock();
            try
            {
                list = actualItems;
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }
        private T ReadList<T>(Func<T> func, ReaderWriterLockSlim lockSlim)
        {
            lockSlim.EnterReadLock();
            try
            {
                return func.Invoke();
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }


        #endregion
    }
}
