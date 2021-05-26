﻿using FlussonnicOrion.Flussonic;
using FlussonnicOrion.Models;
using Newtonsoft.Json;
using System;

namespace FlussonnicOrion
{
    public class FlussonicServer: IFlussonic
    {
        private HttpServer _httpServer;
        public event EventHandler<FlussonicEvent> NewEvent;

        public void Start()
        {
            _httpServer = new HttpServer();
            _httpServer.DataReceived += HttpServer_DataReceived;
            _httpServer.Start("http://172.23.0.128:26038/flussonic_event/"); //TODO: порт должен быть настраиваемый
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
                var flussonicEvents = JsonConvert.DeserializeObject<FlussonicEvent[]>(data);
                foreach (var flussonicEvent in flussonicEvents)
                    NewEvent?.Invoke(this, flussonicEvent);
            }
            catch (Exception ex)
            { 
            
            }
        }
    }
}