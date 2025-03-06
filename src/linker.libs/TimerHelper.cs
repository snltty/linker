using System;
using System.Threading.Tasks;

namespace linker.libs
{
    public static class TimerHelper
    {
        public static void SetTimeout(Action action, int delayMs)
        {
            Task.Run(async () =>
            {
                await Task.Delay(delayMs);
                action();
            });
        }

        public static void SetInterval(Func<bool> action, int delayMs)
        {
            Task.Run(async () =>
            {
                while (action())
                {
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }
            });
        }
        public static void SetIntervalLong(Func<bool> action, int delayMs)
        {
            Task.Factory.StartNew(async () =>
            {
                while (action())
                {
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }
            }, TaskCreationOptions.LongRunning);
        }
        public static void SetInterval(Func<Task<bool>> action, int delayMs)
        {
            Task.Run(async () =>
            {
                while (await action())
                {
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }
            });
        }
        public static void SetIntervalLong(Func<Task<bool>> action, int delay)
        {
            Task.Factory.StartNew(async () =>
            {
                while (await action())
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }, TaskCreationOptions.LongRunning);
        }
        public static void SetInterval(Func<bool> action, Func<int> delay)
        {
            Task.Run(async () =>
            {
                while (action())
                {
                    await Task.Delay(delay()).ConfigureAwait(false);
                }
            });
        }
        public static void SetIntervalLong(Func<bool> action, Func<int> delay)
        {
            Task.Factory.StartNew(async () =>
            {
                while (action())
                {
                    await Task.Delay(delay()).ConfigureAwait(false);
                }
            }, TaskCreationOptions.LongRunning);
        }
        public static void SetInterval(Func<Task<bool>> action, Func<int> delay)
        {
            Task.Run(async () =>
            {
                while (await action())
                {
                    await Task.Delay(delay()).ConfigureAwait(false);
                }
            });
        }
        public static void SetIntervalLong(Func<Task<bool>> action, Func<int> delay)
        {
            Task.Factory.StartNew(async () =>
            {
                while (await action())
                {
                    await Task.Delay(delay()).ConfigureAwait(false);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public static void Async(Action action)
        {
            Task.Run(action);
        }
        public static void Async(Func<Task> action)
        {
            Task.Run(action);
        }
        public static void AsyncLong(Func<Task> action)
        {
            Task.Factory.StartNew(action,TaskCreationOptions.LongRunning);
        }
        
    }
}
