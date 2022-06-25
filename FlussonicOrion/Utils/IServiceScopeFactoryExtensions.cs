using Microsoft.Extensions.DependencyInjection;

namespace FlussonicOrion.Utils
{
    public static class IServiceScopeFactoryExtensions
    {
        public static T Resolve<T>(this IServiceScopeFactory scopeFactory)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                return scope.ServiceProvider.GetRequiredService<T>();
            }
        }
    }
}
