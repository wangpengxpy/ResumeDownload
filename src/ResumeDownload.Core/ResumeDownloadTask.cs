using ResumeDownload.Ext;
using System.Threading;

namespace ResumeDownload.Core
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class ResumeDownloadTask
    {
        /// <summary>
        /// 暂停、继续令牌
        /// </summary>
        internal PauseTokenSource PauseTokenSource { get; set; } = new PauseTokenSource();
        /// <summary>
        /// 取消令牌
        /// </summary>
        internal CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
    }
}
