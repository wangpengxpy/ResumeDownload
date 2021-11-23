using System;
using System.Threading;

namespace ResumeDownload.Core
{
    public class AsyncProgress<T> : IAsyncProgress<T> where T : EventArgs
    {
        private readonly Action<T> handler;

        private readonly SendOrPostCallback invokeHandlers;
        private SynchronizationContext synchronizationContext;

        public AsyncProgress()
        {
            AsyncProgress<T> progress = this;

            var currentExecutionFlow = SynchronizationContext.Current;

            var defaultContext = currentExecutionFlow;

            if (currentExecutionFlow == null)
            {
                defaultContext = AsyncProgressStatics.DefaultContext;
            }

            progress.synchronizationContext = defaultContext;

            invokeHandlers = InvokeHandlers;
        }


        public AsyncProgress(Action<T> handler)
            : this()
        {
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        void IAsyncProgress<T>.Report(T value)
        {
            OnReport(value);
        }

        private void InvokeHandlers(object state)
        {
            var t = (T)state;

            Action<T> mHandler = handler;

            EventHandler<T> eventHandler = ProgressChanged;

            mHandler?.Invoke(t);

            eventHandler?.Invoke(this, t);
        }


        protected virtual void OnReport(T value)
        {
            Action<T> AHandler = handler;

            EventHandler<T> eventHandler = ProgressChanged;

            if (AHandler != null || eventHandler != null)
            {
                synchronizationContext.Post(invokeHandlers, value);
            }
        }

        public event EventHandler<T> ProgressChanged;
    }

    internal static class AsyncProgressStatics
    {
        internal static readonly SynchronizationContext DefaultContext
            = new SynchronizationContext();
    }
}
