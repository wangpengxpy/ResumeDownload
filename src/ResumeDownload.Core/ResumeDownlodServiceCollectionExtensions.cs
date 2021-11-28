using ResumeDownload.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddResumeDownload(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddLogging();

            services.AddSingleton<IConfigureOptions<ResumeDownloadOptions>, ResumeDownloadOptionsSetup>();

            services.AddTransient<BufferManager>();

            services.AddTransient<IDownloadParameters, DownloadParameters>();
            services.AddTransient<IResumeDlownload, ResumeDlownload>();
            services.AddTransient<IResumeDownloadWorker, ResumeDownloadWorker>();

            return services;
        }
    }

    public class ResumeDownloadOptionsSetup : IConfigureOptions<ResumeDownloadOptions>
    {
        public void Configure(ResumeDownloadOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
        }
    }
}
