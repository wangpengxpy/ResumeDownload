using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ResumeDownload.Core
{
    public class DownloadParameters : IDownloadParameters
    {
        private readonly IWebHostEnvironment _hostEnvironment;

        private Lazy<FileStream> fileStream = null;

        public const int DEFAULT_MAX_CHUNK_SIZE = 1000000;

        private readonly ResumeDownloadOptions _options;
        public DownloadParameters(IOptions<ResumeDownloadOptions> options,
            IWebHostEnvironment hostEnvironment)
        {
            _options = options.Value;

            _hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// 下载输出路径
        /// </summary>
        public string OutputFilePath { get; private set; }
        /// <summary>
        /// 下载文件标识
        /// </summary>
        public string Id { get; private set; }
        /// <summary>
        /// 最大分片大小
        /// </summary>
        public int MaxChunkSize { get; private set; }
        /// <summary>
        /// 下载失败最大尝试次数
        /// </summary>
        public int MaxRetries { get; private set; }
        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize { get; private set; }
        /// <summary>
        /// 下载开启最大线程数目
        /// </summary>
        public int MaxThreads { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public Uri Uri { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public string OuputFilePath { get; private set; }

        public void Initial(string id, string url, string outputFilePath)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ResumeDownloadException("下载地址未提供");
            }

            if (string.IsNullOrEmpty(id))
            {
                throw new ResumeDownloadException("文件id未提供");
            }

            if (id.ToCharArray().Count(i => Path.GetInvalidFileNameChars().Any(ic => ic == i)) > 0)
            {
                throw new ResumeDownloadException($"文件名：({id})存在无效字符");
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                throw new ResumeDownloadException($"下载地址“{nameof(url)}”格式不正确");
            }

            Uri = new Uri(url);

            var fileExetionName = Path.GetExtension(url);

            if (string.IsNullOrEmpty(fileExetionName))
            {
                throw new ResumeDownloadException("未找到下载文件扩展名");
            }

            FileSize = Uri.FileSize();

            if (FileSize <= 0)
            {
                throw new ResumeDownloadException("读取文件长度等于0");
            }

            string absoluteOutputPath;

            if (!string.IsNullOrEmpty(outputFilePath))
            {
                if (outputFilePath.StartsWith("/"))
                {
                    absoluteOutputPath = Path.Combine(_hostEnvironment.WebRootPath, outputFilePath.TrimStart('/'));
                }
                else if (IsAbsoluteUrl(outputFilePath))
                {
                    absoluteOutputPath = outputFilePath;
                }
                else
                {
                    absoluteOutputPath = Path.Combine(_hostEnvironment.WebRootPath, outputFilePath);
                }
            }
            else if (!string.IsNullOrEmpty(_options.OutputFilePath))
            {
                if (_options.OutputFilePath.StartsWith("/"))
                {
                    absoluteOutputPath = Path.Combine(_hostEnvironment.WebRootPath, _options.OutputFilePath.TrimStart('/'));
                }
                else if (IsAbsoluteUrl(_options.OutputFilePath))
                {
                    absoluteOutputPath = _options.OutputFilePath;
                }
                else
                {
                    absoluteOutputPath = Path.Combine(_hostEnvironment.WebRootPath, _options.OutputFilePath);
                }
            }
            else
            {
                throw new ResumeDownloadException("文件保存地址未提供");
            }

            Id = id;

            OutputFilePath = $"{absoluteOutputPath.TrimEnd('/')}/{Path.GetFileName(url)}{fileExetionName}";
            fileStream = new Lazy<FileStream>(() => CreateOutputStream(OutputFilePath));
            MaxRetries = _options.MaxRetries;
            MaxThreads = _options.MaxThreads ?? 100;
            MaxChunkSize = FileSize > (_options.MaxChunkSize ?? DEFAULT_MAX_CHUNK_SIZE) ? (_options.MaxChunkSize ?? DEFAULT_MAX_CHUNK_SIZE) : (int)FileSize;
        }

        private bool IsAbsoluteUrl(string path)
        {
            return Path.IsPathRooted(path)
              && !Path.GetPathRoot(path).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }

        public Stream GetOutputStream()
        {
            return fileStream.Value;
        }

        /// <summary>
        /// 默认清除已存在文件，并创建文件输出流
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="deleteIfExists"></param>
        /// <returns></returns>
        private FileStream CreateOutputStream(
            string filePath,
            bool deleteIfExists = true)
        {
            FileStream fileStream;

            try
            {
                EnsuredCleanFile(filePath, deleteIfExists);

                fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }
            catch (Exception ex) when (ex is IOException)
            {
                throw new ResumeDownloadException($"创建文件输出流出现异常：({ex})");
            }

            return fileStream;
        }

        private void EnsuredCleanFile(string filePath, bool deleteIfExists)
        {
            if ((File.Exists(filePath) && deleteIfExists) || !File.Exists(filePath))
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }
    }

    /// <summary>
    /// 获取文件大小
    /// 异常情况未处理
    /// </summary>
    public static class GetFileSizeFromUri
    {
        public static long FileSize(this Uri uri)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(1);
            httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(0, 1);
            long? size;
            try
            {
                var response = httpClient.GetAsync(uri).GetAwaiter().GetResult();
                size = response.Content.Headers.ContentRange.Length;
            }
            catch (Exception ex)
            {
                throw new ResumeDownloadException($"请求获取文件大小出现异常：{ex}");
            }

            return Convert.ToInt64(size ?? 0);
        }
    }
}
