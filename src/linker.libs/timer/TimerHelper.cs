using System;
using System.Threading.Tasks;

namespace linker.libs.timer
{
    public static class TimerHelper
    {
        static HashedWheelTimer timer = new HashedWheelTimer(tickDuration: TimeSpan.FromMilliseconds(30), ticksPerWheel: 100000, maxPendingTimeouts: 0);
        public static void SetTimeout(Action action, int delayMs)
        {
            timer.NewTimeout(new SetTimeout(action),TimeSpan.FromMilliseconds(delayMs));
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

    public sealed class SetTimeout : TimerTask
    {
        private Action action;
        public SetTimeout(Action action)
        {
            this.action = action;
        }

        public void Run(Timeout timeout)
        {
            action.Invoke();
        }
    }
}
