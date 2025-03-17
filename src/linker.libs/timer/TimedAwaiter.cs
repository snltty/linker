using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace linker.libs.timer
{
    public sealed class TimedAwaiter : INotifyCompletion, TimerTask
    {
        public bool IsCompleted { get; private set; }


        private Action _continuation;

        public void OnCompleted(Action continuation)
        {
            _continuation = continuation;
            if (IsCompleted)
                Interlocked.Exchange(ref _continuation, null)?.Invoke();
        }

        public void Run(Timeout timeout)
        {
            IsCompleted = true;
            Interlocked.Exchange(ref _continuation, null)?.Invoke();
        }
        public async Task RunAsync(Timeout timeout)
        {
            await Task.CompletedTask;
        }

        public TimedAwaiter GetAwaiter()
        {
            return this;
        }

        public object GetResult()
        {
            return null;
        }
    }
}
