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
        public ServiceTaskScheduler _scheduler;
        //private static int ACount = 20; //总数
        //private static int BCount = 0;  //完成数
        //private static int CCount = 0;  //失败数
        //private static int DCount = 0;  //无服务数
        public ServiceKeeperHub(ServiceRegistry registry, ServiceTaskScheduler scheduler)
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

        public async Task GetTaskInfo()
        {
            await Clients.All.SendAsync("UpdateTaskInfo", _scheduler.RegisteredTaskCount, _scheduler.ExcutedTaskCount, _scheduler.FailedTaskCount, _scheduler.NotFoundTaskCount);
            _scheduler.ExcutedTaskCount = 0;
            _scheduler.FailedTaskCount = 0;
            _scheduler.NotFoundTaskCount = 0;
            //await Clients.All.SendAsync("UpdateTaskInfo", ACount, BCount,CCount,DCount);
            //BCount = Random.Shared.Next(5, 20);
            //DCount = Random.Shared.Next(0, 2);
            //CCount = ACount - BCount - DCount;
            //await Clients.All.SendAsync("UpdateTaskInfo", _scheduler.RegisteredTaskCount, _scheduler.ExcutedTaskCount);
        }
    }
}
