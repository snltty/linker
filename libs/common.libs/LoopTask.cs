using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace common.libs
{
    public sealed class LoopTask
    {
        List<TaskInfo> tasks = new List<TaskInfo>();
        public LoopTask()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    foreach (TaskInfo item in tasks)
                    {

                    }
                    await Task.Delay(15);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public void AddTask(Action action, int interval)
        {

        }

        sealed class TaskInfo
        {
            public Action Action { get; set; }
            public int Interval { get; set; }
            public int Timer { get; set; }
        }
    }
}
