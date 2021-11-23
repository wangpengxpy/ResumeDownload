namespace ResumeDownload.Core
{
    /// <summary>
    /// 分片文件类
    /// </summary>
    public class DownloadChunkedFilePart
    {
        /// <summary>
        /// 文件偏移量
        /// </summary>
        public long FileOffset { get; set; }
        /// <summary>
        /// 写入长度
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// 写入内容
        /// </summary>
        public byte[] Content { get; set; }
        /// <summary>
        /// 分片大小
        /// </summary>
        public int Chunk { get; set; }
    }
}
