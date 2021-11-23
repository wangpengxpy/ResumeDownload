using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ResumeDownload.Core
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class ResumeDownloadTask
    {
        internal bool IsPaused { get; set; }
        internal bool IsCancelled { get; set; }
        internal List<ResumeDownloadChildTask> ChildTasks { get; set; } = new List<ResumeDownloadChildTask>();
    }

    internal sealed class ResumeDownloadChildTask
    {
        public Task Task { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
    }
}
