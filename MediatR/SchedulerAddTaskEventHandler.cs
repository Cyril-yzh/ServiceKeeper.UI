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
    public class SchedulerAddTaskEventHandler : ISchedulerAddTaskEventHandler
    {
        private readonly FileLoader loader;
        private readonly IHubContext<ServiceKeeperHub> hubContext;
        public SchedulerAddTaskEventHandler(FileLoader loader, IHubContext<ServiceKeeperHub> hubContext)
        {
            this.loader = loader;
            this.hubContext = hubContext;
        }
        /// <summary>
        /// 这是UI对于 AddTask 事件的处理
        /// 会对所有在线前端执行 NewTask 函数
        /// 将新增任务统一发送至前端
        /// 并对任务进行存储
        /// </summary>
        public async Task Handle(SchedulerAddTaskEvent notification, CancellationToken cancellationToken)
        {
            if (notification.TaskEntity != null)
            {
                //存储至指定目录
                loader.Save(notification.TaskEntity, notification.TaskEntity.Detail.Id.ToString());
                //将新任务发送至前端
                await hubContext.Clients.All.SendAsync("AddTask", notification.TaskEntity, cancellationToken: cancellationToken);
            }
            await Task.CompletedTask;
        }

    }
}
