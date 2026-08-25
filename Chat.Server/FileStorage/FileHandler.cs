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
        private readonly Dictionary<Guid, SharedFile> _files;
        private readonly ReaderWriterLockSlim _lock;
        private readonly HashSet<string> _allowedExtensions;
        private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

        public FileHandler()
        {
            _storageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StoredFiles");
            _files = new Dictionary<Guid, SharedFile>();
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
                reason = "File size exceeds the maximum allowed size of 2 MB.";
                return false;
            }

            if (fileSize == 0)
            {
                reason = "File cannot be empty.";
                return false;
            }

            return true;
        }

        public bool StoreFile(string uploaderId, string channelName, string fileName, FileType fileType, byte[] fileData, out string reason, out SharedFile storedFile)
        {
            _lock.EnterWriteLock();
            try
            {
                reason = null;
                storedFile = null;
                string validationReason = null;

                if (fileData == null || !ValidateFile(fileName, fileData == null ? 0 : fileData.Length, fileType, out validationReason))
                {
                    reason = validationReason;
                    return false;
                }

                var fileId = Guid.NewGuid();
                var filePath = Path.Combine(_storageDirectory, fileId.ToString());
                File.WriteAllBytes(filePath, fileData);

                storedFile = new SharedFile
                {
                    FileId = fileId,
                    FileName = fileName,
                    FileType = fileType,
                    FileSize = fileData.Length,
                    UploaderId = uploaderId,
                    UploadedAt = DateTime.UtcNow,
                    ChannelName = channelName,
                    FileData = fileData
                };

                _files[fileId] = storedFile;
                return true;
            }
            catch (Exception ex)
            {
                storedFile = null;
                reason = $"Exception during file storage: {ex.Message}";
                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool StorePrivateFile(string uploaderId, string recipientId, string fileName, FileType fileType, byte[] fileData, out string reason, out SharedFile storedFile)
        {
            _lock.EnterWriteLock();
            try
            {
                reason = null;
                storedFile = null;
                string validationReason = null;
                if (fileData == null || !ValidateFile(fileName, fileData == null ? 0 : fileData.Length, fileType, out validationReason))
                {
                    reason = validationReason;
                    return false;
                }

                var fileId = Guid.NewGuid();
                var filePath = Path.Combine(_storageDirectory, fileId.ToString());
                File.WriteAllBytes(filePath, fileData);
                storedFile = new SharedFile
                {
                    FileId = fileId, FileName = fileName, FileType = fileType,
                    FileSize = fileData.Length, UploaderId = uploaderId,
                    RecipientId = recipientId, UploadedAt = DateTime.UtcNow,
                    ChannelName = null, FileData = fileData
                };
                _files[fileId] = storedFile;
                return true;
            }
            catch (Exception ex)
            {
                storedFile = null;
                reason = $"Exception during file storage: {ex.Message}";
                return false;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public SharedFile GetFile(Guid fileId)
        {
            _lock.EnterReadLock();
            try
            {
                if (_files.ContainsKey(fileId))
                    return _files[fileId];
                return null;
            }
            finally { _lock.ExitReadLock(); }
        }

        public List<SharedFile> GetChannelFiles(string channelName, DateTime visibleFromUtc)
        {
            _lock.EnterReadLock();
            try
            {
                return _files.Values
                    .Where(f => f.ChannelName == channelName && f.UploadedAt >= visibleFromUtc)
                    .Select(f => new SharedFile
                    {
                        FileId = f.FileId,
                        FileName = f.FileName,
                        FileType = f.FileType,
                        FileSize = f.FileSize,
                        UploaderId = f.UploaderId,
                        UploadedAt = f.UploadedAt,
                        ChannelName = f.ChannelName,
                        RecipientId = f.RecipientId,
                        FileData = null // contents served only via GetFile(fileId)
                    })
                    .ToList();
            }
            finally { _lock.ExitReadLock(); }
        }

        public void ClearChannelFiles(string channelName)
        {
            _lock.EnterWriteLock();
            try
            {
                var filesToRemove = _files.Values
                    .Where(f => f.ChannelName == channelName)
                    .Select(f => f.FileId)
                    .ToList();

                foreach (var fileId in filesToRemove)
                {
                    var filePath = Path.Combine(_storageDirectory, fileId.ToString());
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    _files.Remove(fileId);
                }
            }
            finally { _lock.ExitWriteLock(); }
        }
    }
}