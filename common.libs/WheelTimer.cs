using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace common.libs
{
    /// <summary>
    /// 时间轮延时任务
    /// </summary>
    public sealed class WheelTimer<T>
    {
        //流转次数，
        long ticks = 0;
        //槽数
        int bucketLength = 2 << 8;
        //mask = bucketLength-1;ticks & mask; 获得槽下标
        int mask = 0;
        //精度，应使用Thread.Sleep(1)时间的倍数
        int tickDurationMs = 30;
        //槽
        WheelTimerBucket<T>[] buckets = Array.Empty<WheelTimerBucket<T>>();
        //先入列，等待入槽
        ConcurrentQueue<WheelTimerTimeout<T>> timeouts = new ConcurrentQueue<WheelTimerTimeout<T>>();
        AutoResetEvent autoReset = new AutoResetEvent(true);

        public WheelTimer()
        {
            CreateBuckets();
            Worker();
        }
        public WheelTimerTimeout<T> NewTimeout(WheelTimerTimeoutTask<T> task, int delayMs, bool reuse = false)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task must be not null");
            }
            if (delayMs <= 1)
            {
                throw new ArgumentNullException("delayMs must be > 1 ms");
            }

            WheelTimerTimeout<T> timeout = new WheelTimerTimeout<T> { Delay = delayMs, Task = task, Reuse = reuse };
            timeouts.Enqueue(timeout);
            return timeout;
        }

        private void CreateBuckets()
        {
            mask = bucketLength - 1;
            buckets = new WheelTimerBucket<T>[bucketLength];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new WheelTimerBucket<T>();
            }
        }

        double ticksMore = 0;
        private void Worker()
        {
            new Thread(() =>
            {
                for (; ; )
                {
                    long start = DateTime.UtcNow.Ticks;
                    //等下一个时间点
                    WaitForNextTick();
                    //待入槽队列入槽
                    TransferTimeoutsToBuckets();
                    //执行当前槽的任务
                    ExpireTimeouts(buckets[(ticks & mask)]);
                    ticks++;
                    ticksMore += (DateTime.UtcNow.Ticks - start) / TimeSpan.TicksPerMillisecond - tickDurationMs;

                    double forwardCount = (ticksMore / tickDurationMs);
                    while (forwardCount > 1)
                    {
                        ticksMore -= tickDurationMs;

                        start = DateTime.UtcNow.Ticks;
                        //待入槽队列入槽
                        TransferTimeoutsToBuckets();
                        //执行当前槽的任务
                        ExpireTimeouts(buckets[(ticks & mask)]);
                        ticks++;

                        ticksMore += (DateTime.UtcNow.Ticks - start) / TimeSpan.TicksPerMillisecond;

                        forwardCount = ticksMore / tickDurationMs;
                    }
                }

            })
            { IsBackground = true }.Start();
        }
        private void WaitForNextTick()
        {
            autoReset.WaitOne(tickDurationMs);
        }
        private void TransferTimeoutsToBuckets()
        {
            if (timeouts.Count == 0)
            {
                return;
            }
            //一次最多转移100000个
            for (int i = 0; i < 100000; i++)
            {
                if (timeouts.TryDequeue(out WheelTimerTimeout<T> timeout) == false)
                {
                    break;
                }
                if (timeout.IsCanceled)
                {
                    continue;
                }
                //所需格子数
                int expireTicks = (int)((timeout.Delay + ticksMore) / tickDurationMs);
                //所需轮次
                timeout.Rounds = expireTicks / buckets.Length;
                //除轮次外，剩下的格子数应在哪个槽中
                int stopIndex = ((int)(ticks & mask) + (expireTicks - timeout.Rounds * buckets.Length)) & mask;

                buckets[stopIndex].AddTimeout(timeout);
            }
        }
        private void ExpireTimeouts(WheelTimerBucket<T> bucket)
        {
            LinkedListNode<WheelTimerTimeout<T>> timeout = bucket.List.First;
            while (timeout != null)
            {
                bool remove = false;
                if (timeout.Value.Rounds <= 0 && timeout.Value.IsCanceled == false)
                {
                    remove = true;
                    try
                    {
                        timeout.Value.Task.Callback?.Invoke(timeout.Value);
                    }
                    catch (Exception) { }
                }
                else if (timeout.Value.IsCanceled)
                {
                    remove = true;
                }
                else
                {
                    timeout.Value.Rounds--;
                }

                LinkedListNode<WheelTimerTimeout<T>> next = timeout.Next;
                if (remove)
                {
                    bucket.Remove(timeout);
                    if (timeout.Value.Reuse && !timeout.Value.IsCanceled)
                    {
                        timeouts.Enqueue(timeout.Value);
                    }
                }
                timeout = next;
            }
        }
    }

    public sealed class WheelTimerBucket<T>
    {
        public LinkedList<WheelTimerTimeout<T>> List { get; private set; } = new LinkedList<WheelTimerTimeout<T>>();
        public void AddTimeout(WheelTimerTimeout<T> timeout)
        {
            List.AddLast(timeout);
        }
        public void Remove(LinkedListNode<WheelTimerTimeout<T>> node)
        {
            List.Remove(node);
        }
    }

    public sealed class WheelTimerTimeout<T>
    {
        public int Delay { get; init; } = 0;
        public int Rounds { get; set; } = 0;
        public bool Reuse { get; init; } = false;
        public WheelTimerTimeoutTask<T> Task { get; init; }
        public TimeoutState State { get; private set; } = TimeoutState.Normal;
        public bool IsCanceled => State == TimeoutState.Canceld;
        public void Cancel()
        {
            State = TimeoutState.Canceld;
        }

        public enum TimeoutState
        {
            Normal = 1 << 0,
            Canceld = 1 << 1,
        }
    }

    public sealed class WheelTimerTimeoutTask<T>
    {
        /// <summary>
        /// 保存状态数据
        /// </summary>
        public T State { get; init; }
        /// <summary>
        /// 不要抛异常影响轮转时间
        /// </summary>
        public Action<WheelTimerTimeout<T>> Callback { get; init; }
    }
}
