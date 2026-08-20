using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Server.FileStorage
{
    public class FileHandler
    {
        private readonly string _storageDirectory;
        private readonly Dictionary<string, SharedFile> _files;
        private readonly ReaderWriterLockSlim _lock;
        private readonly HashSet<string> _allowedExtensions;
        private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

        public FileHandler()
        {
            _storageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StoredFiles");
            _files = new Dictionary<string, SharedFile>();
            _lock = new ReaderWriterLockSlim();
            _allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".txt"
            };

            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        public bool ValidateFile(string fileName, long fileSize, FileType fileType, out string reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                reason = "File name cannot be empty.";
                return false;
            }

            var extension = Path.GetExtension(fileName);
            if (!_allowedExtensions.Contains(extension))
            {
                reason = $"File type '{extension}' is not allowed. Allowed types: .png, .jpg, .jpeg, .gif, .bmp, .txt";
                return false;
            }

            if (fileSize > MaxFileSizeBytes)
            {
                reason = $"File size exceeds the maximum allowed size of 2 MB.";
                return false;
            }

            if (fileSize == 0)
            {
                reason = "File cannot be empty.";
                return false;
            }

            return true;
        }

        public bool StoreFile(string uploaderId, string channelName, string fileName, FileType fileType, byte[] fileData, out string reason)
        {
            _lock.EnterWriteLock();
            try
            {
                reason = null;

                if (!ValidateFile(fileName, fileData.Length, fileType, out string validationReason))
                {
                    reason = validationReason;
                    return false;
                }

                var fileKey = $"{channelName}_{fileName}";
                if (_files.ContainsKey(fileKey))
                {
                    reason = $"A file named '{fileName}' already exists in this channel.";
                    return false;
                }

                var filePath = Path.Combine(_storageDirectory, fileKey);
                File.WriteAllBytes(filePath, fileData);

                var sharedFile = new SharedFile
                {
                    FileName = fileName,
                    FileType = fileType,
                    FileSize = fileData.Length,
                    UploaderId = uploaderId,
                    UploadedAt = DateTime.UtcNow,
                    ChannelName = channelName,
                    FileData = fileData
                };

                _files[fileKey] = sharedFile;
                return true;
            }
            catch (Exception ex)
            {
                reason = $"Exception during file storage: {ex.Message}";
                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public SharedFile GetFile(string channelName, string fileName)
        {
            _lock.EnterReadLock();
            try
            {
                var fileKey = $"{channelName}_{fileName}";
                if (_files.ContainsKey(fileKey))
                {
                    return _files[fileKey];
                }
                return null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public List<SharedFile> GetChannelFiles(string channelName)
        {
            _lock.EnterReadLock();
            try
            {
                return _files.Values
                    .Where(f => f.ChannelName == channelName)
                    .Select(f => new SharedFile
                    {
                        FileName = f.FileName,
                        FileType = f.FileType,
                        FileSize = f.FileSize,
                        UploaderId = f.UploaderId,
                        UploadedAt = f.UploadedAt,
                        ChannelName = f.ChannelName,
                        FileData = null
                    })
                    .ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void ClearChannelFiles(string channelName)
        {
            _lock.EnterWriteLock();
            try
            {
                var filesToRemove = _files.Keys.Where(k => k.StartsWith($"{channelName}_")).ToList();
                foreach (var fileKey in filesToRemove)
                {
                    var filePath = Path.Combine(_storageDirectory, fileKey);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    _files.Remove(fileKey);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
