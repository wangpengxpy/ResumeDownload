using System;
using System.Threading;
using System.Threading.Tasks;

namespace ResumeDownload.Ext
{
    public static partial class TaskExtensions
    {
        /// <summary>
        /// Waits for the task to complete, unwrapping any exceptions.
        /// </summary>
        /// <param name="task">The task. May not be <c>null</c>.</param>
        public static void WaitAndUnwrapException(this Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Waits for the task to complete, unwrapping any exceptions.
        /// </summary>
        /// <param name="task">The task. May not be <c>null</c>.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was cancelled before the <paramref name="task"/> completed, or the <paramref name="task"/> raised an <see cref="OperationCanceledException"/>.</exception>
        public static void WaitAndUnwrapException(this Task task, CancellationToken cancellationToken)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            try
            {
                task.Wait(cancellationToken);
            }
            catch (AggregateException ex)
            {
                throw ExceptionHelpers.PrepareForRethrow(ex.InnerException);
            }
        }
    }
}
