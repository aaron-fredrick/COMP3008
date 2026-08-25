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
        private readonly IFileContentStore _contentStore;
        private readonly Dictionary<Guid, SharedFile> _files;
        private readonly ReaderWriterLockSlim _lock;
        private readonly HashSet<string> _allowedExtensions;
        private const long MaxFileSizeBytes = 2 * 1024 * 1024;

        public FileHandler() : this(new ShardedFileContentStore(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StoredFiles"))) { }

        public FileHandler(IFileContentStore contentStore)
        {
            _contentStore = contentStore ?? throw new ArgumentNullException(nameof(contentStore));
            _files = new Dictionary<Guid, SharedFile>();
            _lock = new ReaderWriterLockSlim();
            _allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".txt" };
        }

        public bool ValidateFile(string fileName, long fileSize, FileType fileType, out string reason)
        {
            reason = null;
            if (string.IsNullOrWhiteSpace(fileName)) { reason = "File name cannot be empty."; return false; }
            if (fileName.IndexOfAny(new[] { '\\', '/' }) >= 0 || fileName == "." || fileName == "..") { reason = "File name must not contain path separators."; return false; }
            var extension = Path.GetExtension(fileName);
            if (!_allowedExtensions.Contains(extension)) { reason = $"File type '{extension}' is not allowed."; return false; }
            if (fileSize > MaxFileSizeBytes) { reason = "File size exceeds the maximum allowed size of 2 MB."; return false; }
            if (fileSize == 0) { reason = "File cannot be empty."; return false; }
            return true;
        }

        public bool StoreFile(string uploaderId, string channelName, string fileName, FileType fileType, byte[] fileData, out string reason, out SharedFile storedFile)
        {
            reason = null; storedFile = null;
            string validationReason;
            if (fileData == null || !ValidateFile(fileName, fileData == null ? 0 : fileData.Length, fileType, out validationReason)) { reason = validationReason; return false; }
            return StoreInternal(uploaderId, channelName, null, fileName, fileType, fileData, out reason, out storedFile);
        }

        public bool StorePrivateFile(string uploaderId, string recipientId, string fileName, FileType fileType, byte[] fileData, out string reason, out SharedFile storedFile)
        {
            reason = null; storedFile = null;
            string validationReason;
            if (fileData == null || !ValidateFile(fileName, fileData == null ? 0 : fileData.Length, fileType, out validationReason)) { reason = validationReason; return false; }
            return StoreInternal(uploaderId, null, recipientId, fileName, fileType, fileData, out reason, out storedFile);
        }

        private bool StoreInternal(string uploaderId, string channelName, string recipientId, string fileName, FileType fileType, byte[] fileData, out string reason, out SharedFile storedFile)
        {
            reason = null; storedFile = null;
            var fileId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            string storageKey = null;
            try
            {
                storageKey = _contentStore.Store(fileId, fileData);
                var metadata = new SharedFile { FileId = fileId, FileName = fileName, FileType = fileType, FileSize = fileData.Length, UploaderId = uploaderId, UploadedAt = now, LastUpdatedAt = now, ChannelName = channelName, RecipientId = recipientId, StorageKey = storageKey, FileData = null };
                _lock.EnterWriteLock();
                try { _files[fileId] = metadata; storedFile = CopyMetadata(metadata, null); }
                finally { _lock.ExitWriteLock(); }
                return true;
            }
            catch (Exception ex)
            {
                // Store() may have created partial content before throwing, so cleanup is
                // attempted unconditionally. Delete() is idempotent for the filesystem store.
                try { _contentStore.Delete(fileId); } catch { }
                reason = $"Exception during file storage: {ex.Message}";
                return false;
            }
        }

        public SharedFile GetFile(Guid fileId)
        {
            SharedFile metadata;
            _lock.EnterReadLock();
            try { if (!_files.TryGetValue(fileId, out var stored)) return null; metadata = CopyMetadata(stored, null); }
            finally { _lock.ExitReadLock(); }
            var content = _contentStore.Read(fileId);
            return content == null ? null : CopyMetadata(metadata, content);
        }

        public List<SharedFile> GetChannelFiles(string channelName, DateTime visibleFromUtc)
        {
            _lock.EnterReadLock();
            try { return _files.Values.Where(f => f.ChannelName == channelName && f.UploadedAt >= visibleFromUtc).Select(f => CopyMetadata(f, null)).ToList(); }
            finally { _lock.ExitReadLock(); }
        }

        public void ClearChannelFiles(string channelName)
        {
            List<Guid> filesToRemove;
            _lock.EnterWriteLock();
            try
            {
                filesToRemove = _files.Values.Where(f => f.ChannelName == channelName).Select(f => f.FileId).ToList();
                foreach (var fileId in filesToRemove) _files.Remove(fileId);
            }
            finally { _lock.ExitWriteLock(); }
            foreach (var fileId in filesToRemove) { try { _contentStore.Delete(fileId); } catch { } }
        }

        private static SharedFile CopyMetadata(SharedFile file, byte[] content)
        {
            return new SharedFile { FileId = file.FileId, FileName = file.FileName, FileType = file.FileType, FileSize = file.FileSize, UploaderId = file.UploaderId, UploadedAt = file.UploadedAt, LastUpdatedAt = file.LastUpdatedAt, ChannelName = file.ChannelName, RecipientId = file.RecipientId, StorageKey = file.StorageKey, FileData = content };
        }
    }
}
