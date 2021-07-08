﻿using FlussonnicOrion.Flussonic;
using FlussonnicOrion.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace FlussonnicOrion
{
    public class FlussonicServer: IFlussonic
    {
        private ILogger _logger;
        private HttpServer _httpServer;
        public event EventHandler<FlussonicEvent> NewEvent;


        public FlussonicServer(HttpServer server, ILogger logger)
        {
            _logger = logger;
            _httpServer = server;
        }

        private HttpResponse DataReceived(string data)
        {
            Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation(data);

                    var flussonicEvents = JsonConvert.DeserializeObject<FlussonicEvent[]>(data);
                    foreach (var flussonicEvent in flussonicEvents)
                    {
                        NewEvent?.Invoke(this, flussonicEvent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке данных");
                }
            });

            return new HttpResponse(200, null);
        }

        public void Start()
        {
            try
            {
                _httpServer.Subscribe($"http://+:port/flussonic_event/", DataReceived);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запуске FlussonicServer");
            }
        }

        public void Stop()
        {
            _httpServer.Stop();
        }
    }
}
