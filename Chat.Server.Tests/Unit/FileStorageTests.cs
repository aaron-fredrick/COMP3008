using System;
using System.IO;
using System.Linq;
using Chat.Contracts.SharedTypes;
using Chat.Server.FileStorage;
using Chat.Server.Tests.TestInfrastructure;

namespace Chat.Server.Tests.Unit
{
    internal static class FileStorageTests
    {
        public static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "COMP3008-FileStorageTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                ShardedStoreUsesDeterministicPrefixDirectories(root);
                FileHandlerKeepsContentOutOfMetadataAndRestoresItOnRead(root);
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        private static void ShardedStoreUsesDeterministicPrefixDirectories(string root)
        {
            var store = new ShardedFileContentStore(root);
            var id = Guid.Parse("48f57482-3900-4f0d-9bcb-123456789abc");
            var bytes = new byte[] { 1, 2, 3, 4 };
            string key = store.Store(id, bytes);
            string expected = Path.Combine(root, "48", "f5", key + ".blob");

            TestAssert.True(File.Exists(expected), "Content was not stored under the expected two-level shard path");
            TestAssert.True(store.Exists(id), "Stored content should exist");
            TestAssert.True(store.Read(id).SequenceEqual(bytes), "Stored content did not round-trip correctly");
        }

        private static void FileHandlerKeepsContentOutOfMetadataAndRestoresItOnRead(string root)
        {
            var handler = new FileHandler(new ShardedFileContentStore(root));
            string reason;
            var bytes = new byte[] { 10, 20, 30 };
            SharedFileMetadataAssertion(handler, bytes, out Guid fileId, out string storageKey);

            var downloaded = handler.GetFile(fileId);
            TestAssert.True(downloaded != null, "Stored file should be downloadable");
            TestAssert.True(downloaded.FileData.SequenceEqual(bytes), "Downloaded content should match uploaded content");
            TestAssert.True(!string.IsNullOrWhiteSpace(storageKey), "File metadata should expose a stable storage key");
        }

        private static void SharedFileMetadataAssertion(FileHandler handler, byte[] bytes, out Guid fileId, out string storageKey)
        {
            string reason;
            Chat.Contracts.DataContracts.SharedFile stored;
            TestAssert.True(handler.StoreFile("author1", "unit-files", "note.txt", FileType.Text, bytes, out reason, out stored), reason ?? "File storage failed");
            TestAssert.True(stored.FileData == null, "In-memory file metadata should not retain the file bytes");
            TestAssert.Equal("author1", stored.UploaderId, "Uploader metadata should be server-owned");
            TestAssert.True(stored.UploadedAt == stored.LastUpdatedAt, "Initial upload and last-update timestamps should match");
            TestAssert.True(!string.IsNullOrWhiteSpace(stored.StorageKey), "Storage key should be recorded in metadata");
            fileId = stored.FileId;
            storageKey = stored.StorageKey;
        }
    }
}
