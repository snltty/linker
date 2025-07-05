using System;
using System.Threading.Tasks;

namespace linker.libs.timer
{
    public static class TimerHelper
    {
        public static void SetTimeout(Action action, int delayMs)
        {
            Task.Run(async () =>
            {
                await Task.Delay(delayMs).ConfigureAwait(false);
                action();
            });
        }

        public static void SetIntervalLong(Action action, int delayMs)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    action();
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }
            });
        }
        public static void SetIntervalLong(Func<bool> action, int delayMs)
        {
            Task.Run(async () =>
            {
                while (action())
                {
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }
            });
        }
        public static void SetIntervalLong(Func<Task> action, int delayMs)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await action().ConfigureAwait(false);
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }
            });
        }
        public static void SetIntervalLong(Action action, Func<int> delayMs)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    action();
                    await Task.Delay(delayMs()).ConfigureAwait(false);
                }
            });
        }
        public static void SetIntervalLong(Func<Task> action, Func<int> delay)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await action().ConfigureAwait(false);
                    await Task.Delay(delay()).ConfigureAwait(false);
                }
            });
        }
       
        public static void Async(Action action)
        {
            Task.Run(action);
        }
        public static void Async(Func<Task> action)
        {
            Task.Run(action);
        }
    }

}
