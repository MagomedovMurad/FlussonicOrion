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
    public class CrossCamerasFilter: IFilter
    {
        private ILogger _logger;
        private IOrionClient _orionClient;

        private int _id;
        private bool _inProcess;
        private PassRequest _lastPassRequest;
        private ConcurrentQueue<PassRequest> _passRequests;
        private object _requestsHandlingLock = new object();

        public event EventHandler<PassRequest> NewRequest;

        public CrossCamerasFilter(ILogger logger, IOrionClient orionClient)
        {
            _logger = logger;
            _orionClient = orionClient;

            _passRequests = new ConcurrentQueue<PassRequest>();
            _lastPassRequest = new PassRequest()
            {
                LicensePlate = string.Empty,
                EnterInFrameTime = DateTime.Now - TimeSpan.FromDays(1),
                OutFrameTime = DateTime.Now - TimeSpan.FromDays(1)
            };
        }

        public void AddRequest(string licensePlate, PassageDirection direction)
        {
            var request = new PassRequest();
            request.LicensePlate = licensePlate;
            request.EnterInFrameTime = DateTime.Now;
            request.Direction = direction;

            WorkWithPassRequestsQueue(() =>
            {
                _passRequests.Enqueue(request);
                if (!_inProcess)
                {
                    _inProcess = true;
                    Next();
                }
            });
        }
        public void RemoveRequest(string licensePlate)
        {
            if (_lastPassRequest.LicensePlate == licensePlate)
                _lastPassRequest.OutFrameTime = DateTime.Now;
            WorkWithPassRequestsQueue(() => _passRequests = 
                new ConcurrentQueue<PassRequest>(_passRequests.Where(x => x.LicensePlate != licensePlate)));
            _logger.LogInformation($"Удален запрос с номером {licensePlate}");
        }

        private async Task Next()
        {
            if (_passRequests.TryPeek(out PassRequest request))
            {
                var events = await GetEvents();
                var sortedEvents = (events ?? new TEvent[0]).OrderByDescending(x => x.EventDate);

                var lastAccessGrantedEvent = sortedEvents.FirstOrDefault(x => x.EventTypeId == (int)EventType.AccessGranted
                                                                      || x.EventTypeId == (int)EventType.AccessGrantedByKey);

                var lastPassEvent = sortedEvents.FirstOrDefault(x => x.EventTypeId == (int)EventType.PassByKey
                                                  || x.EventTypeId == (int)EventType.Pass);

                if (lastAccessGrantedEvent != null)
                {
                    var passEventIsLast = lastPassEvent?.EventDate > lastAccessGrantedEvent.EventDate;
                    if (NeedAbort(lastAccessGrantedEvent.EventDate,
                             TimeSpan.FromSeconds(20),
                             TimeSpan.FromSeconds(1),
                             "Ожидание после последнего предоставления доступа",
                             !passEventIsLast))
                        return;
                }

                if (lastPassEvent != null)
                {
                    if (NeedAbort(lastPassEvent.EventDate,
                             TimeSpan.FromSeconds(5),
                             null,
                             "Ожидание после последнего прохода"))
                        return;
                }

                var inCamDirection = (lastPassEvent ?? lastAccessGrantedEvent)?.Description?
                                                                               .Contains(request.Direction == PassageDirection.Entry ? "Выход" : "Вход");
                if (inCamDirection is true)
                {
                    _logger.LogInformation($"Последний проезд был в направлении распознавшей камеры");
                    if (_lastPassRequest.LicensePlate == request.LicensePlate)
                    {
                        _logger.LogInformation($"Номер {request.LicensePlate} совпадает с последним запросом");
                        WorkWithPassRequestsQueue(() => RemoveCurrentRequest(request));
                        TryNext();
                        return;
                    }

                    if (NeedAbort(request.EnterInFrameTime,
                            TimeSpan.FromSeconds(5),
                            TimeSpan.FromSeconds(1),
                            "Ожидание нахождения в кадре"))
                        return;
                }

                //Обработать запрос
                _lastPassRequest = request;
                WorkWithPassRequestsQueue(() => RemoveCurrentRequest(request));
                NewRequest.Invoke(this, request);
            }

            TryNext();
        }

        private void TryNext()
        {
            WorkWithPassRequestsQueue(() =>
            {
                if (_passRequests.Count > 0)
                    Task.Delay(3000).ContinueWith(t => Next());
                else
                    _inProcess = false;
            });
        }

        private void WorkWithPassRequestsQueue(Action action)
        {
            lock (_requestsHandlingLock)
                action.Invoke();
        }
        private void RemoveCurrentRequest(PassRequest request)
        {
            _passRequests.TryPeek(out PassRequest current);
            if (request.Equals(current))
                _passRequests.TryDequeue(out PassRequest _);
        }
        private bool NeedAbort(DateTime dateTime, TimeSpan time, TimeSpan? delay, string reason, params bool[] condition)
        {
            var timeFrom = DateTime.Now - dateTime;
            var result = timeFrom < time;
            
            if (condition.Append(result).All(x => x))
            {
                var timeFromDelta = time - timeFrom;
                _logger.LogInformation($"{reason}. Осталось {timeFromDelta} сек из {time}");
                Task.Delay(delay ?? timeFromDelta).ContinueWith(t => Next());
                return true;
            }
            return false;
        }

        //private async Task Next()
        //{
        //    _inProcess = true;

        //    if (_passRequests.TryPeek(out PassRequest request))
        //    {
        //        var events = await GetEvents();
        //        var sortedEvents = (events ?? new TEvent[0]).OrderByDescending(x => x.EventDate);

        //        var lastAccessGrantedEvent = sortedEvents.FirstOrDefault(x => x.EventTypeId == (int)EventType.AccessGranted
        //                                                              || x.EventTypeId == (int)EventType.AccessGrantedByKey);

        //        var lastPassEvent = sortedEvents.FirstOrDefault(x => x.EventTypeId == (int)EventType.PassByKey
        //                                          || x.EventTypeId == (int)EventType.Pass);

        //        if (lastAccessGrantedEvent != null)
        //        {
        //            var timeFromLastAccessGranted = DateTime.Now - lastAccessGrantedEvent.EventDate;
        //            var tty = lastPassEvent?.EventDate > lastAccessGrantedEvent.EventDate;
        //            if (timeFromLastAccessGranted < TimeSpan.FromSeconds(20) && tty == false)
        //            {
        //                var timeFromLastAccessGrantedDelta = TimeSpan.FromSeconds(20) - timeFromLastAccessGranted;
        //                _logger.LogInformation($"Ожидание 20 сек после последнего предоставления доступа {timeFromLastAccessGrantedDelta.Seconds}");
        //                Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(t => Next());
        //                return;
        //            }
        //        }

        //        if (lastPassEvent != null)
        //        {
        //            var timeFromLastPassage = DateTime.Now - lastPassEvent.EventDate;
        //            if (timeFromLastPassage < TimeSpan.FromSeconds(5))
        //            {
        //                var timeFromLastPassageDelta = TimeSpan.FromSeconds(5) - timeFromLastPassage;
        //                _logger.LogInformation($"Ожидание 5 сек после последнего прохода ({timeFromLastPassageDelta})");
        //                Task.Delay(timeFromLastPassageDelta).ContinueWith(t => Next());
        //                return;
        //            }
        //        }

        //        var lastEvent = lastPassEvent ?? lastAccessGrantedEvent;
        //        var inCamDirection = lastEvent?.Description?.Contains(request.Direction == PassageDirection.Entry ? "Выход" : "Вход");
        //        if (inCamDirection is true)
        //        {
        //            _logger.LogInformation($"Последний проезд ({request.LicensePlate}) был в направлении распознавшей камеры");
        //            if (_lastPassRequest.LicensePlate == request.LicensePlate)
        //            {
        //                _logger.LogInformation($"Номер {request.LicensePlate} совпадает с последним запросом");
        //                _inProcess = false;
        //                _passRequests.TryPeek(out PassRequest first);
        //                if (request.Equals(first))
        //                    _passRequests.TryDequeue(out PassRequest _);
        //                return;
        //            }

        //            var timeInFrame = DateTime.Now - request.InFrameTime;
        //            if (timeInFrame < TimeSpan.FromSeconds(5))
        //            {
        //                var timeInFrameDelta = TimeSpan.FromSeconds(5) - timeInFrame;
        //                _logger.LogInformation($"Ожидание нахождения в кадре {timeInFrameDelta.Seconds} из 5 сек");
        //                Task.Delay(timeInFrameDelta).ContinueWith(t => Next());
        //                return;
        //            }
        //        }

        //        //Обработать запрос
        //        _lastPassRequest = request;
        //        _passRequests.TryPeek(out PassRequest first1);
        //        if (request.Equals(first1))
        //            _passRequests.TryDequeue(out PassRequest _);
        //        NewRequest.Invoke(this, request);
        //    }

        //    if (_passRequests.Count > 0)
        //    {
        //        await Task.Delay(3000);
        //        Next();
        //    }
        //    else
        //    {
        //        _inProcess = false;
        //    }
        //}

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
