using System;
using System.Threading.Tasks;

namespace linker.libs.timer
{
    public static class TimerHelper
    {
        private static HashedWheelTimer timer = new HashedWheelTimer(tickDuration: TimeSpan.FromMilliseconds(30), ticksPerWheel: 512, maxPendingTimeouts: 0);
        public static void SetTimeout(Action action, int delayMs)
        {
            timer.NewTimeout(new SetTimeout(action), TimeSpan.FromMilliseconds(delayMs));
        }

        public static void SetIntervalLong(Action action, int delayMs)
        {
            timer.NewTimeout(new SetInterval(action, delayMs), TimeSpan.FromMilliseconds(30));
        }
        public static void SetIntervalLong(Func<Task> action, int delayMs)
        {
            timer.NewTimeout(new SetIntervalAsync(action, delayMs), TimeSpan.FromMilliseconds(30));
        }
        public static void SetIntervalLong(Action action, Func<int> delay)
        {
            timer.NewTimeout(new SetInterval(action, delay), TimeSpan.FromMilliseconds(30));
        }
        public static void SetIntervalLong(Func<Task> action, Func<int> delay)
        {
            timer.NewTimeout(new SetIntervalAsync(action, delay), TimeSpan.FromMilliseconds(30));
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

    public sealed class SetIntervalAsync : TimerTask
    {
        private Func<Task> action;
        private int delayMs;
        private Func<int> delayAction;
        public SetIntervalAsync(Func<Task> action, int delayMs)
        {
            this.action = action;
            this.delayMs = delayMs;
        }
        public SetIntervalAsync(Func<Task> action, Func<int> delayAction)
        {
            this.action = action;
            this.delayAction = delayAction;
        }

        public void Run(Timeout timeout)
        {
        }
        public async Task RunAsync(Timeout timeout)
        {
            await action().ConfigureAwait(false);
            timeout.Timer.NewTimeout(this, TimeSpan.FromMilliseconds(delayAction == null ? delayMs : delayAction()));
        }
    }
    public sealed class SetInterval : TimerTask
    {
        private Action action;
        private int delayMs;
        private Func<int> delayAction;
        public SetInterval(Action action, int delayMs)
        {
            this.action = action;
            this.delayMs = delayMs;
        }
        public SetInterval(Action action, Func<int> delayAction)
        {
            this.action = action;
            this.delayAction = delayAction;
        }

        public void Run(Timeout timeout)
        {
            action.Invoke();
            timeout.Timer.NewTimeout(this, TimeSpan.FromMilliseconds(delayAction == null ? delayMs : delayAction()));
        }
        public async Task RunAsync(Timeout timeout)
        {
            await Task.CompletedTask;
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

        public async Task RunAsync(Timeout timeout)
        {
            await Task.CompletedTask;
        }
    }
}
