using YonatanMankovich.WhatsOnLan.Core.EventArgs;

namespace YonatanMankovich.WhatsOnLan.Core.OUI
{
    /// <summary>
    /// Represents an OUI CSV downloader.
    /// </summary>
    public class OuiCsvDownloader
    {
        private const long MaxDownloadBytes = 10 * 1024 * 1024;

        /// <summary>
        /// The URL of the IEEE OUI CSV file.
        /// </summary>
        public const string IeeeOuiCsvFileUrl = "https://standards-oui.ieee.org/oui/oui.csv";

        private static readonly HttpClient HttpClient = new();

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
        /// <param name="cancellationToken">The token used to cancel the download.</param>
        public async Task DownloadOuiCsvFileAsync(string path, string ouiCsvUrl = IeeeOuiCsvFileUrl,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentException.ThrowIfNullOrWhiteSpace(ouiCsvUrl);

            if (!Uri.TryCreate(ouiCsvUrl, UriKind.Absolute, out Uri? requestUri))
                throw new ArgumentException("The OUI URL must be an absolute URI.", nameof(ouiCsvUrl));

            Progress<int> progress = new Progress<int>((progress) =>
            {
                ProgressChanged?.Invoke(this, new ProgressChangedEventArgs { Progress = progress });
            });

            string destinationPath = Path.GetFullPath(path);
            string temporaryPath = destinationPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (FileStream file = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await DownloadDataAsync(HttpClient, requestUri, file, progress, cancellationToken).ConfigureAwait(false);

                File.Move(temporaryPath, destinationPath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }

            DownloadCompleted?.Invoke(this, System.EventArgs.Empty);
        }

        private static async Task DownloadDataAsync(HttpClient client, Uri requestUri, Stream destination,
            IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            using (var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength > MaxDownloadBytes)
                    throw new InvalidDataException($"The OUI CSV download exceeds the {MaxDownloadBytes / (1024 * 1024)} MB limit.");

                using (var download = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                {
                    // no progress... no contentLength... very sad
                    if (progress is null || !contentLength.HasValue)
                    {
                        await CopyToAsync(download, destination, 81920, maxBytes: MaxDownloadBytes, cancellationToken: cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    // Such progress and contentLength much reporting Wow!
                    var progressWrapper = new Progress<long>(totalBytes
                        => progress.Report((int)Math.Round(100 * (double)totalBytes / contentLength.Value)));
                    await CopyToAsync(download, destination, 81920, progressWrapper, MaxDownloadBytes, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task CopyToAsync(Stream source, Stream destination, int bufferSize, IProgress<long>? progress = null, long maxBytes = long.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (bufferSize <= 0)
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
            while ((bytesRead = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) != 0)
            {
                if (totalBytesRead + bytesRead > maxBytes)
                    throw new InvalidDataException($"The OUI CSV download exceeds the {maxBytes / (1024 * 1024)} MB limit.");

                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }
    }
}
