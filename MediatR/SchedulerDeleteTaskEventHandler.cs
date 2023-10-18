using Microsoft.AspNetCore.SignalR;
using Microsoft.Win32;
using ServiceKeeper.Core;
using ServiceKeeper.Core.MediatR;
using ServiceKeeper.UI.SignalHub;
using ServiceKeeper.UI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.UI.MediatR
{
    public class SchedulerDeleteTaskEventHandler : ISchedulerDeleteTaskEventHandler
    {
        private readonly FileLoader loader;
        private readonly IHubContext<ServiceKeeperHub> hubContext;
        public SchedulerDeleteTaskEventHandler(FileLoader loader, IHubContext<ServiceKeeperHub> hubContext)
        {
            this.loader = loader;
            this.hubContext = hubContext;
        }
        /// <summary>
        /// 这是UI对于 DeleteTask 事件的处理
        /// 会对所有在线前端执行 DeleteTask 函数
        /// 将删除任务ID统一发送至前端
        /// 并对任务进行存储
        /// </summary>
        public async Task Handle(SchedulerDeleteTaskEvent notification, CancellationToken cancellationToken)
        {
            //从指定目录删除对应任务
            loader.Delete(notification.Id.ToString());
            //将任务Id发送至前端
            await hubContext.Clients.All.SendAsync("DeleteTask", notification.Id, cancellationToken: cancellationToken);
            await Task.CompletedTask;
        }

    }
}
