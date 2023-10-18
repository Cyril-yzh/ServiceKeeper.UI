using Microsoft.Extensions.Options;
using ServiceKeeper.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.UI.Utils
{
    internal class TaskLoader
    {
        private readonly FileLoader loader;
        private readonly ServiceScheduler scheduler;
        internal TaskLoader(FileLoader loader, ServiceScheduler taskScheduler)
        {
            this.loader = loader;
            scheduler = taskScheduler;
        }
        public void LoadTask()
        {
            List<TaskEntity> tasks = loader!.LoadAll<TaskEntity>();
            foreach (var task in tasks)
            {
                scheduler.AddTask(task);
            }
        }
    }
}
