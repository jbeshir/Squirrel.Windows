using System;
using System.Net;
using System.Threading.Tasks;
using Splat;

namespace Squirrel
{
    public interface IFileDownloader
    {
        Task DownloadFile(string url, string targetFile);
        Task<byte[]> DownloadUrl(string url);
    }

    class FileDownloader : IFileDownloader, IEnableLogger
    {
        public async Task DownloadFile(string url, string targetFile)
        {
            var wc = Utility.CreateWebClient();
            var failedUrl = default(string);

        retry:
            try {
                this.Log().Info("Downloading file: " + failedUrl ?? url);

                await this.WarnIfThrows(() => wc.DownloadFileTaskAsync(failedUrl ?? url, targetFile),
                    "Failed downloading URL: " + failedUrl ?? url);
            } catch (Exception) {
                // NB: Some super brain-dead services are case-sensitive yet 
                // corrupt case on upload. I can't even.
                if (failedUrl != null) throw;

                failedUrl = url.ToLower();
                goto retry;
            }
        }

        public async Task<byte[]> DownloadUrl(string url)
        {
            var wc = Utility.CreateWebClient();
            var failedUrl = default(string);

        retry:
            try {
                this.Log().Info("Downloading url: " + failedUrl ?? url);

                // Start download on the threadpool,
                // in order to avoid DNS resolution hanging UI thread.
                return await this.WarnIfThrows(async () => {
                    Exception ex = null;
                    byte[] result = await Task.Run(() =>
                    {
                        try
                        {
                            return wc.DownloadDataTaskAsync(failedUrl ?? url);
                        }
                        catch (Exception exOnThread)
                        {
                            ex = exOnThread;
                            return null;
                        }
                    });

                    if (ex != null)
                    {
                        throw ex;
                    }
                    return result;
                }, "Failed to download url: " + failedUrl ?? url);

            } catch (Exception) {
                // NB: Some super brain-dead services are case-sensitive yet 
                // corrupt case on upload. I can't even.
                if (failedUrl != null) throw;

                failedUrl = url.ToLower();
                goto retry;
            }
        }
    }
}
