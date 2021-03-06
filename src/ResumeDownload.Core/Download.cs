using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ResumeDownload.Core
{
    /// <summary>
    /// 开始下载、暂停下载、继续下载管理操作
    /// </summary>
    public static class Download
    {
        internal static ConcurrentDictionary<string, List<ResumeDownloadTask>> Workers { get; set; } =
            new ConcurrentDictionary<string, List<ResumeDownloadTask>>();
    }
}
