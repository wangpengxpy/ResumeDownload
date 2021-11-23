using System;

namespace ResumeDownload.Core
{
    /// <summary>
    /// 自定义下载异常
    /// </summary>
    public class ResumeDownloadException : Exception
    {

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ResumeDownloadException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ResumeDownloadException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResumeDownloadException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ResumeDownloadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
