using FlussonnicOrion.Flussonic;
using FlussonnicOrion.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;

namespace FlussonnicOrion
{
    public class FlussonicServer: IFlussonic
    {
        private ILogger _logger;
        private HttpServer _httpServer;
        private int _port;
        public event EventHandler<FlussonicEvent> NewEvent;


        public FlussonicServer(int port, ILogger logger)
        {
            _logger = logger;
            _port = port;
        }

        public void Start()
        {
            try
            {
                _httpServer = new HttpServer();
                _httpServer.DataReceived += HttpServer_DataReceived;
                _httpServer.Start($"http://+:{_port}/flussonic_event/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запуске FlussonicServer");
            }
        }

        public void Stop()
        {
            _httpServer.DataReceived -= HttpServer_DataReceived;
            _httpServer.Stop();
        }

        private void HttpServer_DataReceived(object sender, string data)
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
        }
    }
}
