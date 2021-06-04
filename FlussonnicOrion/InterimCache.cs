﻿using FlussonnicOrion.OrionPro;
using Orion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace FlussonnicOrion
{
    public class DbCacheController
    {
        private readonly OrionClient _orionClient;

        private List<TVisitData> _visitors;
        private List<TPersonData> _persons;
        private List<TTimeWindow> _timeWindows;

        private Timer _timer;

        public DbCacheController(OrionClient orionClient)
        {
            _orionClient = orionClient;

            _visitors = new List<TVisitData>();
            _persons = new List<TPersonData>();
            _timeWindows = new List<TTimeWindow>();
        }

        public void Start()
        {
            _timer = new Timer(10 * 1000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = false;
            _timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
        }

        public async Task Initialize()
        {
            var visitors = await _orionClient.GetVisits();
            var timeWindows = await _orionClient.GetTimeWindows();
            var persons = await _orionClient.GetPersons(true, 0, 0, null, false, true);
        }

        private IEnumerable<Task> Update()
        {
            yield return _orionClient.GetVisits();
            yield return _orionClient.GetTimeWindows();
            yield return _orionClient.GetPersons(true, 0, 0, null, false, true);
        }

        private async Task<List<TPersonData>> LoadPersons()
        {
            var allPersons = new List<TPersonData>();
            var personsCount = await _orionClient.GetPersonsCount();

            for (int i = 0; i < personsCount; i += 100)
            {
                var persons = await _orionClient.GetPersons(true, i, 100, null, false, false);
                allPersons.AddRange(persons);
            }

            return allPersons;
        }


        private async Task Test()
        {
            var visits = await _orionClient.GetVisits();
            //visits.First().
            var actualVisits = visits.Where(x => x.VisitDate > (DateTime.Now - TimeSpan.FromHours(1)));
            var visitor = actualVisits.FirstOrDefault(x => x.CarNumber == "A777AA26");
            if (visitor.VisitDate <= DateTime.Now && DateTime.Now <= visitor.VisitEndDateTime)
            {
                
            }

            //  await Test2();
            //var personsCount = 500;//await _orionClient.GetPersonsCount();
            //var allPersons = new List<TPersonData>();
            //var test = new List<Task>();
            //for (int i = 0; i < personsCount; i += 100)
            //{
            //    var persons = _orionClient.GetPersons(true, i, 100, null, false, false)
            //                              .ContinueWith(x => allPersons.AddRange(x.Result));
            //    test.Add(persons);
            //}

            //await Task.WhenAll(test);

            //var persons1 = _orionClient.GetPersons(true, 100, 100, null, false, false);
            //var persons2 = _orionClient.GetPersons(true, 200, 100, null, false, false);
            //var persons3 = _orionClient.GetPersons(true, 300, 100, null, false, false);
            //var persons4 = _orionClient.GetPersons(true, 400, 100, null, false, false);

            //var visits = await _orionClient.GetVisits();
            //var personsCount = await _orionClient.GetPersonsCount();
            //var allPersons = new List<TPersonData>();
            //for (int i = 0; i < personsCount; i+=100)
            //{
            //    Console.WriteLine($"{i+1}-{i+100}");
            //    var persons = await _orionClient.GetPersons(true, i, 100, null, false, false);
            //    allPersons.AddRange(persons);
            //}

            //var test = allPersons.Where(x => x.IsInArchive || x.IsDismissed || x.IsInBlackList).ToList();

            ////Попробовать отфильтровать архивные
            //// var persons1 = await _orionClient.GetPersons(true, 0, 0, new[] { "TabNumber=1" }, false, false); //Попробовать отфильтровать архивные
            //var timeWindows = await _orionClient.GetTimeWindows();
        }

        private async Task Test2()
        {
            try
            {
                var keyData = await _orionClient.GetKeyData("Е340РХ126", 5);

                if (keyData.IsBlocked || keyData.IsInStopList || keyData.StartDate > DateTime.Now || keyData.EndDate < DateTime.Now)
                {
                    return;
                }

                var accessLevel = await _orionClient.GetAccessLevelById(keyData.AccessLevelId);
                var accessLevelItem = accessLevel.Items.FirstOrDefault(x => (x.ItemId == 1 || x.ItemId == 0) && x.ItemType == "ACCESSPOINT");

                //accessLevelItem.Rights

                var persons = await _orionClient.GetPersons(true, 0, 0, null, false, false);
                var person = persons.FirstOrDefault(x => x.Id == keyData.PersonId);
                if (person.IsInArchive || person.IsInBlackList || person.IsDismissed)
                {
                    return;
                }
            }
            catch (Exception ex)
            { 
            
            }
        }
    }
}