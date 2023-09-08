using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceKeeper;
using ServiceKeeper.Core;
using System.Reflection;

namespace ServiceKeeper.UI.DependencyInjection
{
    public static class ApplicationBuilderExtensions
    {

        public static IApplicationBuilder UseServiceKeeperUI(this IApplicationBuilder appBuilder)
        {
            IOptions<ServiceKeeperUIOptions> options = appBuilder.ApplicationServices.GetRequiredService<IOptions<ServiceKeeperUIOptions>>();
            ServiceRegistry serviceRegistry = appBuilder.ApplicationServices.GetService<ServiceRegistry>() ?? throw new Exception("ServiceRegistry 获取失败,请先注册 ServiceKeeper");
            ServiceScheduler? scheduler;
            if (serviceRegistry.CurrentOptions.ServiceRole == ServiceRole.Producer)
            {
                scheduler = appBuilder.ApplicationServices.GetService<ServiceScheduler>() ?? throw new Exception("ServiceRegistry 获取失败,请先注册 ServiceKeeper");
            }
            else
            {
                scheduler = null;
            }
            appBuilder.UseRouting();
            appBuilder.UseMiddleware<IndexMiddleware>();
            appBuilder.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ServiceKeeperHub>("/ServiceKeeper/Hub");
            });
            return appBuilder;
        }
    }
}