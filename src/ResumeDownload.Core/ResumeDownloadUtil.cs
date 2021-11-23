using System;

namespace ResumeDownload.Core
{
    /// <summary>
    /// 下载帮助类
    /// </summary>
    public static class ResumeDownloadUtil
    {
        public enum SizeUnits
        {
            Byte, KB, MB, GB, TB, PB, EB, ZB, YB
        }

        public static string ToSize(this long value, SizeUnits unit)
        {
            return (value / (double)Math.Pow(1024, (long)unit)).ToString("0.00");
        }
    }
}
