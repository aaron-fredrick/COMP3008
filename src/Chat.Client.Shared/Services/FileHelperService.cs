using System;
using System.IO;
using System.Diagnostics;

namespace Chat.Client.Shared.Services
{
    public class FileHelperService
    {
        private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2MB

        public long GetFileSize(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return -1;
            }

            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }

        public string GetFileExtension(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return string.Empty;
            }

            return Path.GetExtension(filePath).ToLower();
        }

        public string GetFileName(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return string.Empty;
            }

            return Path.GetFileName(filePath);
        }

        public byte[] ReadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            return File.ReadAllBytes(filePath);
        }

        public bool SaveFile(byte[] fileData, string fileName, string destinationDirectory = null)
        {
            try
            {
                string targetPath = destinationDirectory ?? 
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                string filePath = Path.Combine(targetPath, fileName);
                File.WriteAllBytes(filePath, fileData);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool OpenFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }
            else if (bytes < 1024 * 1024)
            {
                double kb = bytes / 1024.0;
                return $"{Math.Round(kb)} kB";
            }
            else
            {
                double mb = bytes / (1024.0 * 1024.0);
                return $"{Math.Round(mb)} MB";
            }
        }

        public bool IsFileSizeValid(long fileSize)
        {
            return fileSize > 0 && fileSize <= MaxFileSizeBytes;
        }

        public string GetDownloadsPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
    }
}
