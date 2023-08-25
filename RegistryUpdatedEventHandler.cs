using Microsoft.AspNetCore.SignalR;
using Microsoft.Win32;
using ServiceKeeper.Core;
using ServiceKeeper.Core.PendingHandlerMediatREvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.UI
{
    public class RegistryUpdatedEventHandler : IRegistryUpdatedEventHandler
    {
        readonly ServiceRegistry registry;
        readonly IHubContext<ServiceKeeperHub> hubContext;
        public RegistryUpdatedEventHandler(ServiceRegistry registry, IHubContext<ServiceKeeperHub> hubContext)
        {
            this.registry = registry;
            this.hubContext = hubContext;
        }

        public async Task Handle(RegistryUpdatedEvent notification, CancellationToken cancellationToken)
        {
            //将更新后的注册表发给前端
            await hubContext.Clients.All.SendAsync("UpdateServices", registry.Registry, cancellationToken: cancellationToken);
            await Task.CompletedTask;
        }
    }
}
