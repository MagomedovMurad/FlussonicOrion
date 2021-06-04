using Orion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FlussonnicOrion.OrionPro
{
    public class DbCacheController
    {
        private readonly OrionClient _orionClient;

        #region Timers
        private System.Timers.Timer _personsTimer;
        private System.Timers.Timer _visitorsTimer;
        private System.Timers.Timer _timeWindowsTimer;
        #endregion

        #region Items list
        private IEnumerable<TPersonData> _persons;
        private IEnumerable<TVisitData> _visitors;
        private IEnumerable<TTimeWindow> _timeWindows;
        #endregion

        #region Locks
        private ReaderWriterLockSlim _personsLock = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim _visitorsLock = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim _timeWindowsLock = new ReaderWriterLockSlim();
        #endregion

        #region Events
        public event EventHandler<IEnumerable<TPersonData>> PersonsUpdated;
        public event EventHandler<IEnumerable<TVisitData>> VisitorsUpdated;
        public event EventHandler<IEnumerable<TTimeWindow>> TimeWindowsUpdated;
        #endregion
        public DbCacheController(OrionClient orionClient)
        {
            _orionClient = orionClient;
        }

        public void Initialize(int personsInterval, int visitorsInterval, int timeWindowsInterval)
        {
            _personsTimer = CreateTimer(personsInterval, PersonUpdater);
            _visitorsTimer = CreateTimer(visitorsInterval, VisitorsUpdater);
            _timeWindowsTimer = CreateTimer(timeWindowsInterval, TimeWindowsUpdater);

            PersonUpdater(this, null);
            VisitorsUpdater(this, null);
            TimeWindowsUpdater(this, null);
        }

        public void GetVisitorByRegNumber(string regNumber)
        {
            _visitors.FirstOrDefault(x => RegNumbersEquals(x.CarNumber, regNumber));
        }

        private bool RegNumbersEquals(string x, string y)
        { 
            x.ToLower()
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
            _orionClient.GetVisits().ContinueWith(t =>
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
                    UpdateList(t.Result, ref _timeWindows, _timeWindowsLock);

                _timeWindowsTimer.Start();
            });
        }

        private System.Timers.Timer CreateTimer(int interval, ElapsedEventHandler handler)
        {
            var timer = new System.Timers.Timer(interval * 1000);
            timer.Elapsed += handler;
            timer.AutoReset = false;
            return timer;
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

        private async Task<List<TPersonData>> LoadPersons()
        {
            var allPersons = new List<TPersonData>();
            var personsCount = await _orionClient.GetPersonsCount();

            for (int i = 0; i < personsCount; i += 100)
            {
                var persons = await _orionClient.GetPersons(true, i, 100, null, false, false);
                if (persons == null)
                    return null;
                allPersons.AddRange(persons);
            }

            return allPersons;
        }
        #endregion
    }
}
