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

            string sizeStr = totalBytes > 0 ? $"{totalBytes} bytes" : "unknown size";
            Console.WriteLine($"[DOWNLOAD START] {fileName} ({sizeStr})");

            try
            {
                using (var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await destination.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;

                        if (totalBytes > 0)
                        {
                            int percentage = (int)((downloadedBytes * 100) / totalBytes);
                            Console.WriteLine($"[DOWNLOAD] {fileName} {downloadedBytes}/{totalBytes} bytes ({percentage}%)");
                        }
                        else
                        {
                            Console.WriteLine($"[DOWNLOAD] {fileName} {downloadedBytes} bytes downloaded...");
                        }
                    }
                }
                Console.WriteLine($"[DOWNLOAD COMPLETE] {fileName} ({downloadedBytes} bytes total)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DOWNLOAD FAILED] {fileName}: {ex.Message}");
                if (File.Exists(destinationPath))
                {
                    try { File.Delete(destinationPath); } catch { }
                }
                throw;
            }
        }
    }
}
