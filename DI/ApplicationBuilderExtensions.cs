using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceKeeper;
using ServiceKeeper.Core;
using ServiceKeeper.UI.SignalHub;
using ServiceKeeper.UI.Middleware;
using System.Reflection;
using ServiceKeeper.UI.Utils;

namespace ServiceKeeper.UI.DependencyInjection
{
    public static class ApplicationBuilderExtensions
    {

        public static IApplicationBuilder UseServiceKeeperUI(this IApplicationBuilder appBuilder)
        {
            IOptions<ServiceKeeperUIOptions> options = appBuilder.ApplicationServices.GetRequiredService<IOptions<ServiceKeeperUIOptions>>();
            ServiceRegistry serviceRegistry = appBuilder.ApplicationServices.GetService<ServiceRegistry>() ?? throw new Exception("ServiceRegistry 获取失败,请先注册 ServiceKeeper");
            if (serviceRegistry.CurrentOptions.ServiceRole == ServiceRole.Producer)
            {
                if (options.Value.IsTakeOverTaskScheduling)
                {
                    TaskLoader taskLoader = appBuilder.ApplicationServices.GetRequiredService<TaskLoader>();
                    taskLoader.LoadTask();
                }
                appBuilder.UseRouting();
                appBuilder.UseMiddleware<IndexMiddleware>();
                appBuilder.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<ServiceKeeperHub>("/ServiceKeeper/Hub");
                });
            }
            Console.WriteLine($"ServiceKeeperUI: {options.Value.ServiceKeeperUrl}/index.html");
            return appBuilder;
        }
    }
}