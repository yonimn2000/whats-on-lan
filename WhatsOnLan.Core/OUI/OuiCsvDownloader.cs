using YonatanMankovich.WhatsOnLan.Core.EventArgs;

namespace YonatanMankovich.WhatsOnLan.Core.OUI
{
    /// <summary>
    /// Represents an OUI CSV downloader.
    /// </summary>
    public class OuiCsvDownloader
    {
        /// <summary>
        /// The URL of the IEEE OUI CSV file.
        /// </summary>
        public const string IeeeOuiCsvFileUrl = "http://standards-oui.ieee.org/oui/oui.csv";

        /// <summary>
        /// Invoked when the download completes.
        /// </summary>
        public event EventHandler? DownloadCompleted;

        /// <summary>
        /// Invoked when download progress is changed.
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs>? ProgressChanged;

        /// <summary>
        /// Downloads the OUI CSV file from the specified URL to the specified location.
        /// </summary>
        /// <param name="path">The location to download the OUI CSV file to.</param>
        /// <param name="ouiCsvUrl">The URL of the OUI CSV file.</param>
        public async Task DownloadOuiCsvFileAsync(string path, string ouiCsvUrl = IeeeOuiCsvFileUrl)
        {
            HttpClient client = new HttpClient();
            Progress<int> progress = new Progress<int>((progress) =>
            {
                ProgressChanged?.Invoke(this, new ProgressChangedEventArgs { Progress = progress });
            });

            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                await DownloadDataAsync(client, ouiCsvUrl, file, progress);

            DownloadCompleted?.Invoke(this, System.EventArgs.Empty);
        }

        private static async Task DownloadDataAsync(HttpClient client, string requestUrl, Stream destination, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            using (var response = await client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                var contentLength = response.Content.Headers.ContentLength;
                using (var download = await response.Content.ReadAsStreamAsync())
                {
                    // no progress... no contentLength... very sad
                    if (progress is null || !contentLength.HasValue)
                    {
                        await download.CopyToAsync(destination);
                        return;
                    }
                    // Such progress and contentLength much reporting Wow!
                    var progressWrapper = new Progress<long>(totalBytes
                        => progress.Report((int)Math.Round(100 * (double)totalBytes / contentLength.Value)));
                    await CopyToAsync(download, destination, 81920, progressWrapper, cancellationToken);
                }
            }
        }

        private static async Task CopyToAsync(Stream source, Stream destination, int bufferSize, IProgress<long>? progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (!source.CanRead)
                throw new InvalidOperationException($"'{nameof(source)}' is not readable.");
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite)
                throw new InvalidOperationException($"'{nameof(destination)}' is not writable.");

            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }
    }
}