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
                await Task.Delay(delayMs).ConfigureAwait(false);
                action();
            });
        }

        public static void SetIntervalLong(Func<bool> action, int delayMs)
        {
            Task.Factory.StartNew(() =>
            {
                while (action())
                {
                    Task.Delay(delayMs).Wait();
                }
            }, TaskCreationOptions.LongRunning);
        }
        public static void SetIntervalLong(Func<Task<bool>> action, int delay)
        {
            Task.Factory.StartNew(async () =>
            {
                while (await action().ConfigureAwait(false))
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }, TaskCreationOptions.LongRunning);
        }
        public static void SetIntervalLong(Func<bool> action, Func<int> delay)
        {
            Task.Factory.StartNew(() =>
            {
                while (action())
                {
                    Task.Delay(delay()).Wait();
                }
            }, TaskCreationOptions.LongRunning);
        }
        public static void SetIntervalLong(Func<Task<bool>> action, Func<int> delay)
        {
            Task.Factory.StartNew(async () =>
            {
                while (await action().ConfigureAwait(false))
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

    }
}
