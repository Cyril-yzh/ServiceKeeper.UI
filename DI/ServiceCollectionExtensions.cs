using ServiceKeeper.Core.EventBus;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Reflection;
using ServiceKeeper.Core.MQEventHandlers;
using ServiceKeeper.Core.PendingHandlerMediatREvents;
using MediatR.Registration;

namespace ServiceKeeper.UI.DependencyInjection
{

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServiceKeeperUI(this IServiceCollection services)
        {
            services.AddSignalR();
            services.AddSingleton<IRegistryUpdatedEventHandler, RegistryUpdatedEventHandler>();
            return services;
        }
    }
}

