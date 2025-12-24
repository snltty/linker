using System;
using System.Threading;
using System.Threading.Tasks;

namespace linker.libs.extends
{
    public static class TaskCompletionSourceExtends
    {
        public static Task<T> WithTimeout<T>(this TaskCompletionSource<T> tcs, int millisecondsDelay)
        {
            return tcs.WithTimeout(TimeSpan.FromMilliseconds(millisecondsDelay));
        }
        public static Task<T> WithTimeout<T>(this TaskCompletionSource<T> tcs, TimeSpan ts)
        {
            CancellationTokenSource cts = new CancellationTokenSource(ts);

            cts.Token.Register(() =>
            {
                if (tcs.Task.IsCompleted == false)
                {
                    tcs.TrySetCanceled(cts.Token);
                }
            });

            return tcs.Task;
        }
    }
}
