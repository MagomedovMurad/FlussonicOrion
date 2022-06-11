using FlussonicOrion.OrionPro.Enums;
using Microsoft.Extensions.Logging;
using Orion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FlussonicOrion.OrionPro.DataSources
{
    public class OrionCacheDataSource: IOrionDataSource
    {
        private readonly ILogger<IOrionDataSource> _logger;
        private readonly IOrionClient _orionClient;
        private readonly int _updateInterval;

        #region Timers
        private System.Timers.Timer _personsTimer;
        private System.Timers.Timer _visitorsTimer;
        private System.Timers.Timer _keysTimer;
        private System.Timers.Timer _accessLevelsTimer;
        private System.Timers.Timer _timeWindowsTimer;
        #endregion

        #region Items list
        private IEnumerable<TPersonData> _persons;
        private IEnumerable<TVisitData> _visitors;
        private IEnumerable<TKeyData> _keys;
        private IEnumerable<TAccessLevel> _accessLevels;
        private IEnumerable<TTimeWindow> _timeWindows;
        #endregion

        #region Locks
        private ReaderWriterLockSlim _personsLock = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim _visitorsLock = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim _keysLock = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim _accessLevelsLock = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim _timeWindowsLock = new ReaderWriterLockSlim();
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
        public OrionCacheDataSource(int updateInterval, IOrionClient orionClient, ILogger<IOrionDataSource> logger)
        {
            _updateInterval = updateInterval;
            _orionClient = orionClient;
            _logger = logger;
        }

        public void Initialize()
        {
            _logger.LogInformation("Запуск инициализации кэша данных Орион");

            _personsTimer = CreateTimer(PersonUpdater);
            _visitorsTimer = CreateTimer(VisitorsUpdater);
            _keysTimer = CreateTimer(KeysUpdater);
            _accessLevelsTimer = CreateTimer(AccessLevelsUpdater);
            _timeWindowsTimer = CreateTimer(TimeWindowsUpdater);

            PersonUpdater(this, null);
            VisitorsUpdater(this, null);
            KeysUpdater(this, null);
            AccessLevelsUpdater(this, null);
            TimeWindowsUpdater(this, null);

            _logger.LogInformation("Кэш данных Орион инициализирован");
        }
        public void Dispose()
        {
            _personsTimer.Dispose();
            _visitorsTimer.Dispose();
            _keysTimer.Dispose();
            _accessLevelsTimer.Dispose();
            _timeWindowsTimer.Dispose();
        }

        public TVisitData GetActualVisitByRegNumber(string regNumber)
        {
            return ReadList(() => _visitors.Where(x => x.CarNumber.Equals(regNumber))
                                           .FirstOrDefault(x => DateTime.Now >= x.VisitDate &&
                                                                DateTime.Now <= x.VisitEndDateTime), _visitorsLock);
        }
        public TKeyData GetKeyByCode(string code)
        {
            return ReadList(() => _keys.FirstOrDefault(x => x.Code.Equals(code)), _keysLock);
        }
        public TKeyData GetKeyByPersonId(int personId)
        {
            return ReadList(() => _keys.FirstOrDefault(x => x.PersonId.Equals(personId) && 
                                                            x.Comment.Contains("flussonic", 
                                                                               StringComparison.InvariantCultureIgnoreCase)), _keysLock);
        }
        public TPersonData GetPerson(int id)
        {
            return ReadList(() => _persons.FirstOrDefault(x => x.Id.Equals(id)), _personsLock);
        }
        public TAccessLevel GetAccessLevel(int id)
        {
            return ReadList(() => _accessLevels.FirstOrDefault(x => x.Id == id), _accessLevelsLock);
        }
        public TTimeWindow GetTimeWindow(int id)
        {
            return ReadList(() => _timeWindows.FirstOrDefault(x => x.Id == id), _timeWindowsLock);
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
        private void TimeWindowsUpdater(object sender, ElapsedEventArgs e)
        {
            LoadTimeWindows().ContinueWith(t =>
            {
                if (t.Result != null)
                {
                    UpdateList(t.Result, ref _timeWindows, _timeWindowsLock);
                }

                _timeWindowsTimer.Start();
            });
        }

        private async Task<IEnumerable<TPersonData>> LoadPersons()
        {
            var allPersons = new List<TPersonData>();
            var personsCount = await _orionClient.GetPersonsCount(null, true, true);

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
            var keysCount = await _orionClient.GetKeysCount(0,0);

            for (int i = 0; i < keysCount; i += 100)
            {
                var keys = await _orionClient.GetKeys(0, 0, i, 100);
                if (keys == null)
                {
                    _logger.LogError("Ошибка при получении списка TKeyData");
                    return null;
                }
                allKeys.AddRange(keys);
                _logger.LogInformation($"Получение списка TKeyData: {allKeys.Count} из {keysCount}");
            }

            allKeys.Where(x => x.CodeType == (int)CodeType.CarNumber)
                   .ToList()
                   .ForEach(x => x.Code = ReplaceCirilicToLatin(x.Code));

            return allKeys;
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
        private async Task<IEnumerable<TTimeWindow>> LoadTimeWindows()
        {
            var timeWindows = await _orionClient.GetTimeWindows();
            if (timeWindows == null)
            {
                _logger.LogError("Ошибка при получении списка TTimeWindow");
                return null;
            }
            _logger.LogInformation($"Получение списка TTimeWindow: {timeWindows.Length} из {timeWindows.Length}");
            return timeWindows;
        }

        private System.Timers.Timer CreateTimer(ElapsedEventHandler handler)
        {
            var timer = new System.Timers.Timer(_updateInterval * 1000);
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
