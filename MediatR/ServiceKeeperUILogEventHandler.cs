using Microsoft.AspNetCore.SignalR;
using Microsoft.Win32;
using Newtonsoft.Json;
using Serilog;
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
    public class ServiceKeeperUILogEventHandler : IServiceKeeperLogEventHandler
    {
        public ServiceKeeperUILogEventHandler(){ }
        public Task Handle(ServiceKeeperLog notification, CancellationToken cancellationToken)
        {
            if (notification.Level == LogLevel.Debug) Log.Debug(notification.Message);
            else if (notification.Level == LogLevel.Info) Log.Information(notification.Message);
            else if (notification.Level == LogLevel.Warning) Log.Warning(notification.Message);
            else if (notification.Level == LogLevel.Error) Log.Error(notification.Message);
            else if (notification.Level == LogLevel.Fatal) Log.Fatal(notification.Message);
            return Task.CompletedTask;
        }
    }
}
