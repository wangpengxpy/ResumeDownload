using System.Threading.Tasks;

namespace ResumeDownload.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface IResumeDownloadWorker
    {
        /// <summary>
        /// 开始下载
        /// </summary>
        /// <param name="url">下载路径</param>
        /// <param name="id">文件id</param>
        /// <param name="outputFilePath">输出路径</param>
        /// <param name="progress">文件id</param>
        /// <returns></returns>
        Task Start(string url, string id, string outputFilePath = "", IAsyncProgress<DownloadProgressChangedEventArgs> progress = null);
        /// <summary>
        /// 暂停下载
        /// </summary>
        /// <param name="id">文件id</param>
        /// <returns></returns>
        void Pause(string id);
        /// <summary>
        /// 继续下载
        /// </summary>
        /// <param name="id">文件id</param>
        /// <returns></returns>
        void Continue(string id);
        /// <summary>
        /// 取消
        /// </summary>
        /// <param name="id"></param>
        void Cancell(string id);
    }
}
