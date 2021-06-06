using FlussonnicOrion.Flussonic;
using FlussonnicOrion.Models;
using Newtonsoft.Json;
using System;

namespace FlussonnicOrion
{
    public class FlussonicServer: IFlussonic
    {
        private HttpServer _httpServer;
        private int _port;
        public event EventHandler<FlussonicEvent> NewEvent;


        public FlussonicServer(int port)
        {
            _port = port;
        }

        public void Start()
        {
            _httpServer = new HttpServer();
            _httpServer.DataReceived += HttpServer_DataReceived;
            _httpServer.Start($"http://+:{_port}/flussonic_event/");
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
                Console.WriteLine(data);
                Console.WriteLine();

                var flussonicEvents = JsonConvert.DeserializeObject<FlussonicEvent[]>(data);
                foreach (var flussonicEvent in flussonicEvents)
                {
                    NewEvent?.Invoke(this, flussonicEvent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
