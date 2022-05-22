using FlussonnicOrion.Models;
using FlussonnicOrion.OrionPro;
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

        public event EventHandler<PassRequest> NewRequest;

        public CrossCamerasFilter(ILogger logger, IOrionClient orionClient)
        {
            _logger = logger;
            _orionClient = orionClient;

            _passRequests = new ConcurrentQueue<PassRequest>();
            _lastPassRequest = new PassRequest()
            {
                LicensePlate = string.Empty,
                InFrameTime = DateTime.Now - TimeSpan.FromDays(1),
                OutFrameTime = DateTime.Now - TimeSpan.FromDays(1)
            };
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
            if (_lastPassRequest.LicensePlate == licensePlate)
                _lastPassRequest.OutFrameTime = DateTime.Now;
            _passRequests = new ConcurrentQueue<PassRequest>(_passRequests.Where(x => x.LicensePlate != licensePlate));
            _logger.LogInformation($"Удален запрос с номером {licensePlate}");
        }

        private async Task Next()
        {
            _inProcess = true;

            if (_passRequests.TryPeek(out PassRequest request))
            {
                if (_lastPassRequest.OutFrameTime == null)
                {
                    Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(t => Next());
                    _logger.LogInformation($"Ожидание выхода {_lastPassRequest.LicensePlate} из кадра");
                    return;
                }                                                                                                                                

                var timeFromLastLeaveFrame = DateTime.Now - _lastPassRequest.OutFrameTime;
                if (timeFromLastLeaveFrame < TimeSpan.FromSeconds(5))
                {
                    var timeFromLastLeaveFrameDelta = TimeSpan.FromSeconds(5) - timeFromLastLeaveFrame.Value;
                    Task.Delay(timeFromLastLeaveFrameDelta).ContinueWith(t => Next());
                    _logger.LogInformation($"Ожидание {timeFromLastLeaveFrameDelta.Seconds} из 5 сек после выхода {_lastPassRequest.LicensePlate} из кадра");
                    return;
                }
                var events = await GetEvents();
                var lastEvent = (events ?? new TEvent[0]).OrderByDescending(x => x.EventDate).FirstOrDefault();

                if (lastEvent != null)
                {
                    if (lastEvent.Description.Contains(request.Direction == PassageDirection.Entry ? "Выход" : "Вход"))
                    {
                        _logger.LogInformation($"Запрошен проезд ({request.LicensePlate}) в направлении распознавшей камеры");
                        if (_lastPassRequest.LicensePlate == request.LicensePlate)
                        {
                            _logger.LogInformation($"Номер {request.LicensePlate} совпадает с последним запросом");
                            _inProcess = false;
                            _passRequests.TryDequeue(out PassRequest _);
                            return;
                        }

                        var timeInFrame = DateTime.Now - request.InFrameTime;
                        if (timeInFrame < TimeSpan.FromSeconds(5))
                        {
                            var timeInFrameDelta = TimeSpan.FromSeconds(5) - timeInFrame;
                            _logger.LogInformation($"Ожидание нахождения в кадре {timeInFrameDelta.Seconds} из 5 сек");
                            Task.Delay(timeInFrameDelta).ContinueWith(t => Next());
                            return;
                        }
                    }

                    var timeFromLastPassage = DateTime.Now - lastEvent.EventDate;
                    if (timeFromLastPassage < TimeSpan.FromSeconds(15))
                    {
                        var timeFromLastPassageDelta = TimeSpan.FromSeconds(15) - timeFromLastPassage;
                        _logger.LogInformation($"Ожидание после последнего прохода {timeFromLastPassageDelta.Seconds} из 15 сек");
                        Task.Delay(timeFromLastPassageDelta).ContinueWith(t => Next());
                        return;
                    }
                }

                //Обработать запрос
                _lastPassRequest = request;
                _passRequests.TryDequeue(out PassRequest _);
                NewRequest.Invoke(this, request);
            }

            if (_passRequests.Count > 0)
                Next();
            else
                _inProcess = false;
        }

        private async Task<TEvent[]> GetEvents()
        {
            var entryPoints = new[] { _id };
            var eventTypes = new[] { 32, 271 };

            return await _orionClient.GetEvents(DateTime.Now - TimeSpan.FromSeconds(30),
                DateTime.Now, eventTypes, 0, 0, null, entryPoints, null, null);
        }
    }
}
