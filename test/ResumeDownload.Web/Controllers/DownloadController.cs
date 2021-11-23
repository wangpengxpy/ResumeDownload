using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ResumeDownload.Core;
using ResumeDownload.Web.Hubs;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ResumeDownload.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DownloadController : ControllerBase
    {
        private readonly IResumeDownloadWorker _resumeDownload;

        private static ConcurrentQueue<DownloadProgressBar> ProgressBars =
            new ConcurrentQueue<DownloadProgressBar>();

        private IHubContext<ProgressHub> _hubContext;
        private readonly ILogger<HomeController> _logger;
        public DownloadController(ILogger<HomeController> logger,
            IHubContext<ProgressHub> hubContext,
            IResumeDownloadWorker resumeDownload)
        {
            _logger = logger;
            _hubContext = hubContext;

            _resumeDownload = resumeDownload;
        }

        [HttpPost]
        public IActionResult Video()
        {
            var progress = new AsyncProgress<DownloadProgressChangedEventArgs>();

            _ = Task.Run(() =>
              {
                  _resumeDownload.Start("https://www.hnsdwl.com/mongo.zip", progress: progress);
              });

            progress.ProgressChanged += Progress_ProgressChanged;

            return Ok();

        }

        /// <summary>
        /// 暂停
        /// </summary>
        [HttpPost]
        public void Pause()
        {
            _resumeDownload.Pause("mongo.zip");
        }

        /// <summary>
        /// 继续
        /// </summary>
        [HttpPost]
        public void Continue()
        {
            _resumeDownload.Continue("mongo.zip");
        }

        /// <summary>
        /// 取消
        /// </summary>
        [HttpPost]
        public void Cancell()
        {
            _resumeDownload.Cancell("mongo.zip");
        }

        private void Progress_ProgressChanged(object sender,
            DownloadProgressChangedEventArgs e)
        {
            if (e == null || e.IsFailed)
            {
                return;
            }

            ProgressBars.Enqueue(e.GetProgressBar());

            while (!ProgressBars.IsEmpty && ProgressBars.Count > 0 &&
            ProgressBars.TryDequeue(out var progressBar))
            {
                _hubContext.Clients.All.SendAsync("progress", progressBar);
            }
        }
    }
}
