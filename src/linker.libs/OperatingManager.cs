using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace linker.libs
{
    public sealed class OperatingManager
    {
        private uint operating;
        public bool Operating => operating == 1;

        public bool StartOperation()
        {
            return Interlocked.CompareExchange(ref operating, 1, 0) == 0;
        }
        public void StopOperation()
        {
            Interlocked.Exchange(ref operating, 0);
        }
    }

    public sealed class OperatingMultipleManager
    {
        public ConcurrentDictionary<string, bool> StringKeyValue => dicOperating;

        public VersionManager DataVersion { get; } = new VersionManager();

        private readonly ConcurrentDictionary<string, bool> dicOperating = new ConcurrentDictionary<string, bool>();
        public bool TryGetValue(string key, out bool result)
        {
            return dicOperating.TryGetValue(key, out result);
        }

        public bool StartOperation(string key)
        {
            DataVersion.Increment();
            return dicOperating.TryAdd(key, true);
        }
        public bool StartOperation(string key, Func<Task> func)
        {
            DataVersion.Increment();
            if (dicOperating.TryAdd(key, true))
            {
                func().ContinueWith(t =>
                {
                    StopOperation(key);
                }).ConfigureAwait(false);
                return true;
            }
            return false;
        }
        public async Task<bool> StartOperationAsync(string key, Func<Task> func)
        {
            DataVersion.Increment();
            if (dicOperating.TryAdd(key, true))
            {
                try
                {
                    await func().ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
                finally
                {
                    StopOperation(key);
                }
                return true;
            }
            return false;
        }
        public async Task<T> StartOperationAsync<T>(string key, T defaultReturnValue, Func<Task<T>> func)
        {
            DataVersion.Increment();
            if (dicOperating.TryAdd(key, true))
            {
                try
                {
                    return await func().ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
                finally
                {
                    StopOperation(key);
                }
            }
            return defaultReturnValue;
        }
        public async Task<T> StartOperationAsync<T>(string key, bool autoStop, Func<string, Task<T>> hasIn, Func<string, Task<T>> hasOut)
        {
            DataVersion.Increment();

            Func<string, Task<T>> func = dicOperating.TryAdd(key, true) ? hasIn : hasOut;
            try
            {
                return await func(key).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
            finally
            {
                if (autoStop)
                    StopOperation(key);
            }
            return default(T);
        }
        public async Task StartOperationAsync(string key, bool autoStop, Func<string, Task> hasIn, Func<string, Task> hasOut)
        {
            DataVersion.Increment();

            Func<string, Task> func = dicOperating.TryAdd(key, true) ? hasIn : hasOut;
            try
            {
                await func(key).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
            finally
            {
                if (autoStop)
                    StopOperation(key);
            }
        }
        public void StopOperation(string key)
        {
            DataVersion.Increment();
            dicOperating.TryRemove(key, out _);
        }
    }
}
