using System;
using System.Collections.Generic;
using System.Threading;

namespace GreyOTron.Library.Helpers
{
    public sealed class TimeSpanSemaphore : IDisposable
    {
        private readonly TimeSpan resetSpan;
        private readonly SemaphoreSlim pool;
        private readonly Queue<DateTime> releaseTimes;
        private readonly object queueLock = new object();

        public TimeSpanSemaphore(int maxCount, TimeSpan resetSpan)
        {
            this.resetSpan = resetSpan;
            pool = new SemaphoreSlim(maxCount, maxCount);
            releaseTimes = new Queue<DateTime>(maxCount);
            for (int i = 0; i < maxCount; i++)
            {
                releaseTimes.Enqueue(DateTime.MinValue);
            }
        }

        private void Wait(CancellationToken cancellationToken)
        {
            pool.Wait(cancellationToken);
            DateTime oldestRelease;
            lock (queueLock)
            {
                oldestRelease = releaseTimes.Dequeue();
            }

            var now = DateTime.UtcNow;
            var windowReset = oldestRelease.Add(resetSpan);
            if (windowReset > now)
            {
                var sleep = Math.Max((int)(windowReset.Subtract(now).Ticks / TimeSpan.TicksPerMillisecond), 1);
                var cancelled = cancellationToken.WaitHandle.WaitOne(sleep);
                if (cancelled)
                {
                    Release();
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        private void Release()
        {
            lock (queueLock)
            {
                releaseTimes.Enqueue(DateTime.UtcNow);
            }
            pool.Release();
        }

        public int CurrentCount => pool.CurrentCount;

        public void Run(Action action, CancellationToken cancellationToken)
        {
            Wait(cancellationToken);

            try
            {
                action();
            }
            finally
            {
                Release();
            }
        }

        public void Dispose()
        {
            pool.Dispose();
        }
    }
}
