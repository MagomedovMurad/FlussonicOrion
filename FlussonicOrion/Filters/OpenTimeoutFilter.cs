﻿using FlussonicOrion.Models;
using FlussonicOrion.OrionPro;
using FlussonicOrion.OrionPro.Enums;
using Microsoft.Extensions.Logging;
using Orion;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace FlussonicOrion.Filters
{
    public class OpenTimeoutFilter : IFilter
    {
        private ILogger _logger;
        private IOrionClient _orionClient;

        private int _accessPointId;
        private bool _inProcess;
        private ConcurrentQueue<PassRequest> _passRequests;
        private object _requestsHandlingLock = new object();
        private FilterSettings _settings;
        private TimeSpan _requestedEventsInterval;

        public event EventHandler<PassRequest> NewRequest;

        public OpenTimeoutFilter(int accessPointId, ILogger logger,
                                 IOrionClient orionClient,
                                 IServiceSettingsController serviceSettingsController)
        {
            _accessPointId = accessPointId;
            _logger = logger;
            _orionClient = orionClient;
            _settings = serviceSettingsController.Settings.FilterSettings;
            _passRequests = new ConcurrentQueue<PassRequest>();

            var requestedEventsIntervalSec = new[]
            {
                _settings.TimeAfterLastAccessGranted,
                _settings.TimeAfterLastPass
            }.Max() + 10;
            _requestedEventsInterval = TimeSpan.FromSeconds(requestedEventsIntervalSec);
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
            WorkWithPassRequestsQueue(() =>
            {
                if (_passRequests.Any(x => x.LicensePlate == licensePlate))
                {
                    _passRequests = new ConcurrentQueue<PassRequest>(_passRequests.Where(x => x.LicensePlate != licensePlate));
                    _logger.LogInformation($"Удален запрос с номером {licensePlate} т.к покинул кадр");
                }
            });
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
                             TimeSpan.FromSeconds(_settings.TimeAfterLastAccessGranted),
                             TimeSpan.FromSeconds(1),
                             "Ожидание после последнего предоставления доступа",
                             !passEventIsLast))
                        return;
                }

                if (lastPassEvent != null)
                {
                    if (NeedAbort(lastPassEvent.EventDate,
                              TimeSpan.FromSeconds(_settings.TimeAfterLastPass),
                              null,
                              "Ожидание после последнего прохода"))
                        return;
                }

                //Обработать запрос
                WorkWithPassRequestsQueue(() => RemoveCurrentRequest(request));
                NewRequest.Invoke(this, request);
            }

            WorkWithPassRequestsQueue(() =>
            {
                if (_passRequests.Count > 0)
                    Task.Delay(TimeSpan.FromSeconds(_settings.EventLogDelay)).ContinueWith(t => Next());
                else
                    _inProcess = false;
            });
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
        private async Task<TEvent[]> GetEvents()
        {
            var entryPoints = new[] { _accessPointId };
            var eventTypes = new[] 
            { 
                (int)EventType.Pass, 
                (int)EventType.PassByKey, 
                (int)EventType.AccessGranted, 
                (int)EventType.AccessGrantedByKey 
            };

            return await _orionClient.GetEvents(
                DateTime.Now - _requestedEventsInterval,
                DateTime.Now + TimeSpan.FromSeconds(5), 
                eventTypes, 0, 0, null, 
                entryPoints, null, null);
        }
    }
}