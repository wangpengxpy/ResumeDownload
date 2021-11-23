using System;

namespace ResumeDownload.Core
{
    /// <summary>
    /// 报告下载进度
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAsyncProgress<in T> where T : EventArgs
    {
        void Report(T value);
    }
}
