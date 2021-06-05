using FlussonnicOrion.Api;
using FlussonnicOrion.OrionPro;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FlussonnicOrion
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var api = new FlussonicApi();
            //api.ExecuteRequest("http://10.21.48.160/vsaas/api/v2/events?type=activity&source=plate_detector");
            var t = new OrionClient();
            var settingsController = new ServiceSettingsController();
            settingsController.Initialize();
            //await t.Initialize(IPAddress.Parse("10.21.101.19"), userName: "skip", password: "master123");
            await t.Initialize(settingsController.Settings.OrionSettings); // IPAddress.Parse("172.20.5.51"), userName: "admin", password: "password", tokenLogin: "admin123", tokenPassword: "password", IsTokenRequired: true);
            await t.Test();
           
            //var tt = new FlussonicServer(26038);
            //tt.NewEvent += Tt_NewEvent;
            //tt.Start();
            ////_logger.LogError("Test");
            Console.ReadKey();
        }

        private void Tt_NewEvent(object sender, Models.FlussonicEvent e)
        {
            
        }
    }
}
