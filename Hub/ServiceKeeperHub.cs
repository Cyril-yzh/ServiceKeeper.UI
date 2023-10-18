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

namespace ServiceKeeper.UI.SignalHub
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
        public async Task GetServicesInfo()
        {
            await Clients.All.SendAsync("InitServicesInfo", _registry.Registry);
        }
        /// <summary>
        /// 初始化任务执行情况至前端
        /// </summary>
        /// <returns></returns>
        public async Task GetTotalTaskStatus()
        {
            await Clients.All.SendAsync("UpdateTotalTaskStatus", _scheduler.RegisteredTaskCount, _scheduler.TotalExecutedTaskCount, _scheduler.TotalSuccessTaskCount
              , _scheduler.TotalFailedTaskCount, _scheduler.TotalNotFoundTaskCount);
        }
        /// <summary>
        /// 获取一段时间内的任务处理情况
        /// </summary>
        public async Task GetTimeRangeTaskStatus()
        {
            await Clients.All.SendAsync("UpdateTimeRangeStatus", _scheduler.TimeRangeExecutedTaskCount, _scheduler.TimeRangeSuccessTaskCount, _scheduler.TimeRangeFailedTaskCount
                , _scheduler.TimeRangeNotFoundTaskCount);
            _scheduler.TimeRangeExecutedTaskCount = 0;
            _scheduler.TimeRangeSuccessTaskCount = 0;
            _scheduler.TimeRangeFailedTaskCount = 0;
            _scheduler.TimeRangeNotFoundTaskCount = 0;
        }
        /// <summary>
        /// 初始化任务信息至前端
        /// </summary>
        public async Task GetTasksInfo()
        {
            await Clients.All.SendAsync("InitTasksInfo", _scheduler.Tasks);
        }
    }
}
