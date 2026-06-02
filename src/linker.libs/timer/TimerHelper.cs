using System;
using System.Threading;
using System.Threading.Tasks;

namespace linker.libs.timer
{
    public static class TimerHelper
    {
        public static void SetTimeout(Action action, int delayMs, CancellationToken cts = default)
        {
            Task.Run(async () =>
            {
                await Task.Delay(delayMs, cts).ConfigureAwait(false);
                action();
            }, cts);
        }

        public static void SetIntervalLong(Action action, int delayMs, CancellationToken token = default)
        {
            Task.Run(async () =>
            {
                using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(delayMs));
                do
                {
                    action();
                }
                while (await timer.WaitForNextTickAsync(token));
            }, token);
        }
        public static void SetIntervalLong(Func<bool> action, int delayMs, CancellationToken token = default)
        {
            Task.Run(async () =>
            {
                using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(delayMs));
                do
                {
                    if (action() == false)
                    {
                        break;
                    }
                }
                while (await timer.WaitForNextTickAsync(token));

            }, token);
        }
        public static void SetIntervalLong(Func<Task> action, int delayMs, CancellationToken token = default)
        {
            Task.Run(async () =>
            {
                using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(delayMs));
                do
                {
                    await action().ConfigureAwait(false);
                }
                while (await timer.WaitForNextTickAsync(token));

            }, token);
        }
        public static void SetIntervalLong(Action action, Func<int> delayMs, CancellationToken token = default)
        {
            Task.Run(async () =>
            {
                using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(delayMs()));
                do
                {
                    action();
                    timer.Period = TimeSpan.FromMilliseconds(delayMs());
                }
                while (await timer.WaitForNextTickAsync(token));

            }, token);
        }
        public static void SetIntervalLong(Func<Task> action, Func<int> delayMs, CancellationToken token = default)
        {
            Task.Run(async () =>
            {
                using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(delayMs()));
                do
                {

                    await action().ConfigureAwait(false);
                    timer.Period = TimeSpan.FromMilliseconds(delayMs());
                }
                while (await timer.WaitForNextTickAsync(token));

            }, token);
        }

        public static void Async(Action action, CancellationToken cts = default)
        {
            Task.Run(action, cts);
        }
        public static void Async(Func<Task> action, CancellationToken cts = default)
        {
            Task.Run(action, cts);
        }
    }
}
