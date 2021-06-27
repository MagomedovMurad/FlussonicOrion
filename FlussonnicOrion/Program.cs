using FlussonnicOrion.Controllers;
using FlussonnicOrion.OrionPro;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlussonnicOrion
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
                    services.AddHostedService<Worker>()
                            .AddSingleton<ILogicController, LogicController>()
                            .AddSingleton<IServiceSettingsController, ServiceSettingsController>()
                            .AddSingleton<IOrionClient, OrionClient>()
                            .AddSingleton<OrionClientDataSource, OrionClientDataSource>()
                            .AddSingleton<OrionCacheDataSource, OrionCacheDataSource>()
                            .AddSingleton<IAccesspointsCache, AccesspointsCache>();
                });
    }
}
