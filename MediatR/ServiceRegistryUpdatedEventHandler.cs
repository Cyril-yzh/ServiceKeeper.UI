using Microsoft.AspNetCore.SignalR;
using Microsoft.Win32;
using ServiceKeeper.Core;
using ServiceKeeper.Core.MediatR;
using ServiceKeeper.UI.SignalHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.UI.MediatR
{
    public class ServiceRegistryUpdatedEventHandler : IServiceRegistryUpdatedEventHandler
    {
        readonly ServiceRegistry registry;
        readonly IHubContext<ServiceKeeperHub> hubContext;
        public ServiceRegistryUpdatedEventHandler(ServiceRegistry registry, IHubContext<ServiceKeeperHub> hubContext)
        {
            this.registry = registry;
            this.hubContext = hubContext;
        }

        public async Task Handle(ServiceRegistryUpdatedEvent notification, CancellationToken cancellationToken)
        {
            //将更新后的注册表发给前端
            await hubContext.Clients.All.SendAsync("InitServicesInfo", registry.Registry, cancellationToken: cancellationToken);
            await Task.CompletedTask;
        }
    }
}
