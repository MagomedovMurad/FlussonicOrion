using FlussonicOrion.Controllers;
using FlussonicOrion.Managers;
using FlussonicOrion.Models;
using FlussonicOrion.OrionPro;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlussonicOrion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Service>()
                            .AddSingleton<IServiceSettingsController, ServiceSettingsController>()
                            .AddSingleton<IOrionClient, OrionClient>()
                            .AddSingleton<OrionClientDataSource, OrionClientDataSource>()
                            .AddSingleton<OrionCacheDataSource, OrionCacheDataSource>()
                            .AddSingleton<IAccessPointsManager, AccessPointsManager>()
                            .AddSingleton<IVideoSourceManager, VideoSourceManager>();
                });
    }
}
