using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    public static class SemaphoreExtensions
    {
        public static SemaphoreDisposer DisposableWait(this Semaphore semaphore, CancellationToken cancellationToken)
        {
            if (cancellationToken.CanBeCanceled)
            {
                int signalledIndex = WaitHandle.WaitAny(new[] {semaphore, cancellationToken.WaitHandle});
                if (signalledIndex != 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw ExceptionUtilities.Unreachable;
                }
            }
            else
            {
                semaphore.WaitOne();
            }

            return new SemaphoreDisposer(semaphore);
        }

        public static Task<SemaphoreDisposer> DisposableWaitAsync(this Semaphore semaphore, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(
                function: () => DisposableWait(semaphore, cancellationToken),
                cancellationToken: cancellationToken,
                creationOptions: TaskCreationOptions.LongRunning,
                scheduler: TaskScheduler.Default);
        }

        public struct SemaphoreDisposer : IDisposable
        {
            private readonly Semaphore _semaphore;

            public SemaphoreDisposer(Semaphore semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
