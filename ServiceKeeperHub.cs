using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System;
using ServiceKeeper.Core;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.UI
{
    public class ServiceKeeperHub : Hub
    {
        //public IOptions<ServiceKeeperUIOptions> _options;
        public ServiceRegistry _registry;
        public ServiceScheduler _scheduler;
        public ServiceKeeperHub(ServiceRegistry registry, ServiceScheduler scheduler)
        {
            _registry = registry;
            _scheduler = scheduler;
        }

        /// <summary>
        /// 初始化服务器信息至前端
        /// </summary>
        public async Task GetServices()
        {
            await Clients.All.SendAsync("UpdateServices", _registry.Registry);
        }
        /// <summary>
        /// 获取一段时间内的任务处理情况
        /// </summary>
        public async Task GetTaskStatus()
        {
            await Clients.All.SendAsync("UpdateTaskInfo", _scheduler.RegisteredTaskCount, _scheduler.ExcutedTaskCount, _scheduler.FailedTaskCount, _scheduler.NotFoundTaskCount);
            _scheduler.ExcutedTaskCount = 0;
            _scheduler.FailedTaskCount = 0;
            _scheduler.NotFoundTaskCount = 0;
        }
        public async Task Getaxxx()
        {

        }
    }
}
