using FlussonnicOrion.Models;
using FlussonnicOrion.OrionPro;
using FlussonnicOrion.OrionPro.Enums;
using Microsoft.Extensions.Logging;
using Orion;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace FlussonnicOrion.Filters
{
    public class OpenTimeoutFilter : IFilter
    {
        private ILogger _logger;
        private IOrionClient _orionClient;

        private int _id;
        private bool _inProcess;
        private ConcurrentQueue<PassRequest> _passRequests;        

        public event EventHandler<PassRequest> NewRequest;

        public OpenTimeoutFilter(ILogger logger, IOrionClient orionClient)
        {
            _logger = logger;
            _orionClient = orionClient;
            _passRequests = new ConcurrentQueue<PassRequest>();
        }

        public void AddRequest(string licensePlate, PassageDirection direction)
        {
            var request = new PassRequest();
            request.LicensePlate = licensePlate;
            request.InFrameTime = DateTime.Now;
            request.Direction = direction;
            _passRequests.Enqueue(request);

            if (!_inProcess)
                Next();
        }

        public void RemoveRequest(string licensePlate)
        {
            _passRequests = new ConcurrentQueue<PassRequest>(_passRequests.Where(x => x.LicensePlate != licensePlate));
            _logger.LogInformation($"Удален запрос с номером {licensePlate}");
        }

        private async Task Next()
        {
            _inProcess = true;

            if (_passRequests.TryPeek(out PassRequest request))
            {
                var events = await GetEvents();
                var sortedEvents = (events ?? new TEvent[0]).OrderByDescending(x => x.EventDate);
                var lastPassEvent = sortedEvents.FirstOrDefault(x => x.EventTypeId == (int)EventType.PassByKey 
                                                                  || x.EventTypeId == (int)EventType.Pass);

                var lastAccessGrantedEvent = sortedEvents.FirstOrDefault(x => x.EventTypeId == (int)EventType.AccessGranted 
                                                                      || x.EventTypeId == (int)EventType.AccessGrantedByKey);


                if (lastAccessGrantedEvent != null)
                {
                    var timeFromLastAccessGranted = DateTime.Now - lastAccessGrantedEvent.EventDate;
                    var tty = lastPassEvent?.EventDate > lastAccessGrantedEvent.EventDate;
                    if (timeFromLastAccessGranted < TimeSpan.FromSeconds(20) && tty == false)
                    {
                        var timeFromLastAccessGrantedDelta = TimeSpan.FromSeconds(20) - timeFromLastAccessGranted;
                        _logger.LogInformation($"Ожидание 20 сек после последнего предоставления доступа {timeFromLastAccessGrantedDelta.Seconds}");
                        Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(t => Next());
                        return;
                    }
                }

                if (lastPassEvent != null)
                {
                    var timeFromLastPassage = DateTime.Now - lastPassEvent.EventDate;
                    if (timeFromLastPassage < TimeSpan.FromSeconds(5))
                    {
                        var timeFromLastPassageDelta = TimeSpan.FromSeconds(5) - timeFromLastPassage;
                        _logger.LogInformation($"Ожидание 5 сек после последнего прохода ({timeFromLastPassageDelta})");
                        Task.Delay(timeFromLastPassageDelta).ContinueWith(t => Next());
                        return;
                    }
                }

                //Обработать запрос
                _passRequests.TryPeek(out PassRequest first);
                if (request.Equals(first))
                    _passRequests.TryDequeue(out PassRequest _);

                NewRequest.Invoke(this, request);
            }

            if (_passRequests.Count > 0)
            {
                await Task.Delay(3000);
                Next();
            }
            else
                _inProcess = false;
        }

        private async Task<TEvent[]> GetEvents()
        {
            var entryPoints = new[] { _id };
            var eventTypes = new[] 
            { 
                (int)EventType.Pass, 
                (int)EventType.PassByKey, 
                (int)EventType.AccessGranted, 
                (int)EventType.AccessGrantedByKey 
            };

            return await _orionClient.GetEvents(DateTime.Now - TimeSpan.FromSeconds(30),
                DateTime.Now, eventTypes, 0, 0, null, entryPoints, null, null);
        }
    }
}
