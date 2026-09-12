using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;
using Chat.Server.FileStorage;
using Chat.Server.Tests.TestInfrastructure;

namespace Chat.Server.Tests.Unit
{
    [TestClass]
    public class FileStorageTests
    {
        public static void Run()
        {
            var suite = new FileStorageTests();
            suite.ShardedStoreUsesDeterministicPrefixDirectories();
            suite.FileHandlerKeepsContentOutOfMetadataAndRestoresItOnRead();
            suite.ValidationRejectsEmptyAndOversizedFiles();
            suite.ValidationRejectsUnsafeAndUnsupportedNames();
            suite.ExactlyTwoMegabytesIsAccepted();
            suite.FailedContentWriteDoesNotLeaveMetadata();
            suite.FileIoDoesNotHoldMetadataLock();
            suite.ClearChannelRemovesMetadataAndContent();
        }

        private static string CreateTestTempDir()
        {
            var root = Path.Combine(Path.GetTempPath(), "COMP3008-FileStorageTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ShardedStoreUsesDeterministicPrefixDirectories()
        {
            var root = CreateTestTempDir();
            try
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
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void FileHandlerKeepsContentOutOfMetadataAndRestoresItOnRead()
        {
            var root = CreateTestTempDir();
            try
            {
                var handler = new FileHandler(new ShardedFileContentStore(root));
                var bytes = new byte[] { 10, 20, 30 };
                SharedFileMetadataAssertion(handler, bytes, out Guid fileId, out string storageKey);
                var downloaded = handler.GetFile(fileId);
                TestAssert.True(downloaded != null, "Stored file should be downloadable");
                TestAssert.True(downloaded.FileData.SequenceEqual(bytes), "Downloaded content should match uploaded content");
                TestAssert.True(!string.IsNullOrWhiteSpace(storageKey), "File metadata should expose a stable storage key");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        private static void SharedFileMetadataAssertion(FileHandler handler, byte[] bytes, out Guid fileId, out string storageKey)
        {
            string reason;
            SharedFile stored;
            TestAssert.True(handler.StoreFile("author1", "unit-files", "note.txt", FileType.Txt, bytes, out reason, out stored), reason ?? "File storage failed");
            TestAssert.True(stored.FileData == null, "In-memory file metadata should not retain the file bytes");
            TestAssert.Equal("author1", stored.UploaderId, "Uploader metadata should be server-owned");
            TestAssert.True(stored.UploadedAt == stored.LastUpdatedAt, "Initial upload and last-update timestamps should match");
            TestAssert.True(!string.IsNullOrWhiteSpace(stored.StorageKey), "Storage key should be recorded in metadata");
            TestAssert.Equal((long)bytes.Length, stored.FileSize, "Metadata file size should match content length");
            fileId = stored.FileId;
            storageKey = stored.StorageKey;
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ValidationRejectsEmptyAndOversizedFiles()
        {
            var root = CreateTestTempDir();
            try
            {
                var handler = new FileHandler(new ShardedFileContentStore(Path.Combine(root, "validation")));
                string reason; SharedFile file;
                TestAssert.False(handler.StoreFile("u", "c", "empty.txt", FileType.Txt, new byte[0], out reason, out file), "Empty file should be rejected");
                TestAssert.True(reason != null && reason.Contains("empty"), "Empty-file rejection should explain the reason");
                TestAssert.False(handler.StoreFile("u", "c", "large.txt", FileType.Txt, new byte[(2 * 1024 * 1024) + 1], out reason, out file), "Files above 2 MB should be rejected");
                TestAssert.True(reason != null && reason.Contains("2 MB"), "Oversize rejection should identify the 2 MB limit");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ValidationRejectsUnsafeAndUnsupportedNames()
        {
            var root = CreateTestTempDir();
            try
            {
                var handler = new FileHandler(new ShardedFileContentStore(Path.Combine(root, "names")));
                string reason; SharedFile file;
                TestAssert.False(handler.StoreFile("u", "c", "../../escape.txt", FileType.Txt, new byte[] { 1 }, out reason, out file), "Path traversal filename should be rejected");
                TestAssert.False(handler.StoreFile("u", "c", "nested\\escape.txt", FileType.Txt, new byte[] { 1 }, out reason, out file), "Backslash path filename should be rejected");
                TestAssert.False(handler.StoreFile("u", "c", "payload.exe", FileType.Txt, new byte[] { 1 }, out reason, out file), "Unsupported extension should be rejected");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ExactlyTwoMegabytesIsAccepted()
        {
            var root = CreateTestTempDir();
            try
            {
                var handler = new FileHandler(new ShardedFileContentStore(Path.Combine(root, "boundary")));
                string reason; SharedFile file;
                var data = new byte[2 * 1024 * 1024];
                TestAssert.True(handler.StoreFile("u", "c", "boundary.txt", FileType.Txt, data, out reason, out file), reason ?? "Exactly 2 MB should be accepted");
                TestAssert.Equal((long)data.Length, file.FileSize, "2 MB file metadata size is incorrect");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void FailedContentWriteDoesNotLeaveMetadata()
        {
            var store = new FailingContentStore();
            var handler = new FileHandler(store);
            string reason; SharedFile file;
            TestAssert.False(handler.StoreFile("u", "c", "failed.txt", FileType.Txt, new byte[] { 1 }, out reason, out file), "A failed content write should fail the upload");
            TestAssert.True(file == null, "Failed content write must not publish metadata");
            TestAssert.Equal(1, store.DeleteCalls, "Failed content write should attempt cleanup");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void FileIoDoesNotHoldMetadataLock()
        {
            var store = new BlockingContentStore();
            var handler = new FileHandler(store);
            string reason; SharedFile file;
            var worker = new Thread(() => handler.StoreFile("u", "c", "slow.txt", FileType.Txt, new byte[] { 1 }, out reason, out file));
            worker.Start();
            TestAssert.True(store.StoreStarted.Wait(2000), "Blocking content store did not start");
            var completed = false;
            var probe = new Thread(() => { handler.GetChannelFiles("c", DateTime.UtcNow.AddHours(-1)); completed = true; });
            probe.Start();
            TestAssert.True(probe.Join(1000), "Metadata operation was blocked by slow content I/O");
            TestAssert.True(completed, "Metadata operation should complete while content I/O is blocked");
            store.Release.Set();
            worker.Join(2000);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ClearChannelRemovesMetadataAndContent()
        {
            var root = CreateTestTempDir();
            try
            {
                var store = new ShardedFileContentStore(Path.Combine(root, "clear"));
                var handler = new FileHandler(store);
                string reason; SharedFile file;
                TestAssert.True(handler.StoreFile("u", "clear-channel", "remove.txt", FileType.Txt, new byte[] { 1, 2 }, out reason, out file), reason ?? "Setup failed");
                TestAssert.True(store.Exists(file.FileId), "Content should exist before channel cleanup");
                handler.ClearChannelFiles("clear-channel");
                TestAssert.False(store.Exists(file.FileId), "Channel cleanup should delete physical content");
                TestAssert.True(handler.GetChannelFiles("clear-channel", DateTime.UtcNow.AddHours(-1)).Count == 0, "Channel cleanup should remove metadata");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        private sealed class FailingContentStore : IFileContentStore
        {
            public int DeleteCalls { get; private set; }
            public string Store(Guid fileId, byte[] content) { throw new IOException("simulated content-store failure"); }
            public byte[] Read(Guid fileId) { return null; }
            public bool Exists(Guid fileId) { return false; }
            public void Delete(Guid fileId) { DeleteCalls++; }
        }

        private sealed class BlockingContentStore : IFileContentStore
        {
            public readonly ManualResetEventSlim StoreStarted = new ManualResetEventSlim(false);
            public readonly ManualResetEventSlim Release = new ManualResetEventSlim(false);
            public string Store(Guid fileId, byte[] content) { StoreStarted.Set(); Release.Wait(5000); return fileId.ToString("N"); }
            public byte[] Read(Guid fileId) { return new byte[] { 1 }; }
            public bool Exists(Guid fileId) { return true; }
            public void Delete(Guid fileId) { }
        }
    }
}
