using System;
using System.ComponentModel;

namespace ResumeDownload.Core
{
    public class DownloadProgressChangedEventArgs : ProgressChangedEventArgs
    {
        public DownloadProgressChangedEventArgs(
            int progressPercentage,
            object userState,
            bool isFailed = false) : base(progressPercentage, userState)
        {
            IsFailed = isFailed;
        }

        public DownloadProgressChangedEventArgs(
            long totalFileSize,
            int progressPercentage,
            double downloadBitRate,
            double writeBitRate,
            long bytesWritten,
            long bytesDownloaded,
            string url,
            string id,
            object userState,
            bool isFailed = false,
            string reasonForFailure = "")
            : base(progressPercentage, userState)
        {
            TotalFlieSize = totalFileSize;
            DownloadBitRate = downloadBitRate;
            WriteBitRate = writeBitRate;
            BytesWritten = bytesWritten;
            BytesDownloaded = bytesDownloaded;
            Url = url;
            Id = id;
            IsFailed = isFailed;
            ReasonForFailure = reasonForFailure;
        }

        #region Properties
        /// <summary>
        /// 文件总大小（M）
        /// </summary>
        public long TotalFlieSize { get; private set; }
        /// <summary>
        /// 下载的瞬时比特率，以每秒位数为单位。
        /// </summary>
        public double DownloadBitRate { get; private set; }
        /// <summary>
        ///每秒写入位的瞬时位速率。
        /// </summary>
        public double WriteBitRate { get; private set; }
        /// <summary>
        /// 写入磁盘的字节数
        /// </summary>
        public long BytesWritten { get; private set; }
        /// <summary>
        /// 下载的字节数
        /// </summary>
        public long BytesDownloaded { get; private set; }
        /// <summary>
        /// 与此下载关联的文件URL。
        /// </summary>
        public string Url { get; private set; }
        /// <summary>
        /// 文件id
        /// </summary>
        public string Id { get; private set; }
        /// <summary>
        /// 当前是否下载成功（如果为false，则下载失败）
        /// </summary>
        public bool IsFailed { get; private set; }
        /// <summary>
        /// 下载失败原因
        /// </summary>
        public string ReasonForFailure { get; private set; }

        #endregion

        public DownloadProgressBar GetProgressBar()
        {
            return new DownloadProgressBar()
            {
                Id = Id,
                TotalSize = $"{TotalFlieSize.ToSize(ResumeDownloadUtil.SizeUnits.MB)}M",
                Percentage = ProgressPercentage,
                DownloadSize = $"{BytesDownloaded.ToSize(ResumeDownloadUtil.SizeUnits.MB)}M",
                DownloadRate = $@"{Convert.ToInt64(DownloadBitRate / 8)
                .ToSize(ResumeDownloadUtil.SizeUnits.KB)}KB/s"
            };
        }
    }
}
