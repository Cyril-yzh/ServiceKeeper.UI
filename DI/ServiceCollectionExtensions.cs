using ServiceKeeper.Core.EventBus;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Reflection;
using ServiceKeeper.Core.MQEventHandlers;
using ServiceKeeper.Core.MediatR;
using MediatR.Registration;
using ServiceKeeper.UI.MediatR;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Options;
using ServiceKeeper.Core;
using Serilog.Formatting.Compact;
using ServiceKeeper.UI.Utils;
using Microsoft.AspNetCore.SignalR;
using ServiceKeeper.UI.SignalHub;

namespace ServiceKeeper.UI.DependencyInjection
{

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServiceKeeperUI(this IServiceCollection services)
        {
            services.AddSignalR().AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = false;
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            });
            //MaximumReceiveMessageSize //单个传入中心消息的最大大小。 提高此值可能会增加拒绝服务(DoS) 攻击风险。
            services.AddSingleton<IServiceRegistryUpdatedEventHandler, ServiceRegistryUpdatedEventHandler>();
            services.AddSingleton<IServiceKeeperLogEventHandler, ServiceKeeperUILogEventHandler>();

            var options = services.BuildServiceProvider().GetRequiredService<IOptions<ServiceKeeperUIOptions>>().Value;
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{options.LogDirName}");
            string logFilePath = Path.Combine(logDirectory, "log-.txt");
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .WriteTo.Console()
            .WriteTo.File(
                new CompactJsonFormatter(),
                logFilePath,
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

            if (options.IsTakeOverTaskScheduling)//UI主动接管任务调度和日志
            {   
                services.AddSingleton(sp => //添加FileLoader来为任务的读取保存提供基础支持
                {
                    return new FileLoader(options.TaskDirName);
                });
                services.AddSingleton(sp => //添加TaskLoader来初始化任务调度,将保存的任务配置添加调度器
                {
                    ServiceScheduler serviceScheduler = sp.GetRequiredService<ServiceScheduler>();
                    FileLoader loader = sp.GetRequiredService<FileLoader>();
                    return new TaskLoader(loader, serviceScheduler);
                });
                services.AddSingleton(sp => //添加LogLoader来为日志的读取提供基础支持
                {
                    return new LogLoader(options);
                });
                services.AddSingleton<ISchedulerAddTaskEventHandler, SchedulerAddTaskEventHandler>(sp =>
                {                           //添加SchedulerAddTaskEventHandler实现对Core的新增任务进行侦听
                    FileLoader loader = sp.GetRequiredService<FileLoader>();
                    IHubContext<ServiceKeeperHub> hubContext = sp.GetRequiredService<IHubContext<ServiceKeeperHub>>();
                    return new SchedulerAddTaskEventHandler(loader, hubContext);
                });
                services.AddSingleton<ISchedulerDeleteTaskEventHandler, SchedulerDeleteTaskEventHandler>(sp =>
                {                           //添加SchedulerDeleteTaskEventHandler实现对Core的删除任务进行侦听
                    FileLoader loader = sp.GetRequiredService<FileLoader>();
                    IHubContext<ServiceKeeperHub> hubContext = sp.GetRequiredService<IHubContext<ServiceKeeperHub>>();
                    return new SchedulerDeleteTaskEventHandler(loader, hubContext);
                });
            }
            return services;
        }
    }
}

