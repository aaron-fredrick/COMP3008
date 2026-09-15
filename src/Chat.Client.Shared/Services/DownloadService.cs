using System;
using System.IO;
using System.Threading.Tasks;

namespace Chat.Client.Shared.Services
{
    public class DownloadService
    {
        public async Task DownloadAsync(Stream source, string destinationPath, long totalBytes, string fileName)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));

            long downloadedBytes = 0;
            byte[] buffer = new byte[81920]; // 80 KB bounded buffer
            int bytesRead;

            try
            {
                using (var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await destination.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;
                    }
                }
            }
            catch
            {
                if (File.Exists(destinationPath))
                {
                    try { File.Delete(destinationPath); } catch { }
                }
                throw;
            }
        }
    }
}
