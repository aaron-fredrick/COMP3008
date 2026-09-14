using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;
using Chat.Client.Shared.Services;
using Chat.Client.Tests.Helpers;

namespace Chat.Client.Tests.Unit.Export
{
    [TestClass]
    public class ChatExportServiceTests
    {
        private string _tempDir;
        private ChatExportService _service;

        [TestInitialize]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "COMP3008-ClientTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _service = new ChatExportService();
        }

        [TestCleanup]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        // ── Transcript formatting ─────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        public void NormalMessage_AppearsInTranscript()
        {
            var messages = MessageBuilder.Sequence(
                MessageBuilder.Text("alice", "hello world", new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)));

            var zip = RunExport("general", messages, _ => null);
            var transcript = ReadTranscriptFromZip(zip);

            StringAssert.Contains(transcript, "alice");
            StringAssert.Contains(transcript, "hello world");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void FileMessage_ShowsFileOmittedInTranscript()
        {
            var fileId = Guid.NewGuid();
            var messages = MessageBuilder.Sequence(
                MessageBuilder.File("alice", "report.txt", fileId));

            var zip = RunExport("general", messages, id => id == fileId ? new byte[] { 1, 2, 3 } : null);
            var transcript = ReadTranscriptFromZip(zip);

            StringAssert.Contains(transcript, "<File omitted>");
            Assert.IsFalse(transcript.Contains("report.txt"),
                "Transcript must not include the file name inline — use <File omitted>");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void OwnMessage_SenderIdIsPreservedInTranscript()
        {
            var messages = MessageBuilder.Sequence(
                MessageBuilder.OwnText("bob", "my own message"));

            var zip = RunExport("general", messages, _ => null);
            var transcript = ReadTranscriptFromZip(zip);

            StringAssert.Contains(transcript, "bob");
            StringAssert.Contains(transcript, "my own message");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void TimestampFormat_IsCorrect()
        {
            var utcTime = new DateTime(2024, 3, 7, 14, 5, 0, DateTimeKind.Utc);
            var messages = MessageBuilder.Sequence(
                MessageBuilder.Text("alice", "ts test", utcTime));

            var zip = RunExport("general", messages, _ => null);
            var transcript = ReadTranscriptFromZip(zip);

            // Expected: "07/03/2024, HH:mm" (local time conversion, so just check structure)
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(transcript, @"\d{2}/\d{2}/\d{4}, \d{2}:\d{2}"),
                "Transcript timestamp must match dd/MM/yyyy, HH:mm format");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void MessagesAreInChronologicalOrder()
        {
            var t1 = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
            var t2 = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
            var messages = MessageBuilder.Sequence(
                MessageBuilder.Text("alice", "first", t1),
                MessageBuilder.Text("bob", "second", t2));

            var zip = RunExport("general", messages, _ => null);
            var transcript = ReadTranscriptFromZip(zip);

            int posFirst = transcript.IndexOf("first", StringComparison.Ordinal);
            int posSecond = transcript.IndexOf("second", StringComparison.Ordinal);
            Assert.IsTrue(posFirst < posSecond, "Messages must appear in chronological order");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void EmptyChannel_ProducesZipWithTranscriptOnly()
        {
            var zip = RunExport("empty-channel", new List<Chat.Client.Shared.ViewModels.ConversationItemViewModel>(), _ => null);

            using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
            {
                Assert.AreEqual(1, archive.Entries.Count, "Empty channel ZIP must contain exactly 1 entry (the transcript)");
                Assert.IsTrue(archive.Entries[0].Name.EndsWith(".txt"), "The single entry must be the transcript .txt file");
            }
        }

        // ── ZIP structure ─────────────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        public void TranscriptFileName_ContainsChannelName()
        {
            var zip = RunExport("my-channel", new List<Chat.Client.Shared.ViewModels.ConversationItemViewModel>(), _ => null);

            using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
            {
                bool hasTxt = archive.Entries.Any(e => e.Name.Contains("my-channel") && e.Name.EndsWith(".txt"));
                Assert.IsTrue(hasTxt, "The transcript file name must contain the channel name");
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void File_IsIncludedInZip_WhenBytesAreAvailable()
        {
            var fileId = Guid.NewGuid();
            var payload = new byte[] { 10, 20, 30 };
            var messages = MessageBuilder.Sequence(
                MessageBuilder.File("alice", "doc.txt", fileId));

            var zip = RunExport("general", messages, id => id == fileId ? payload : null);

            using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
            {
                var fileEntry = archive.Entries.FirstOrDefault(e => e.Name == "doc.txt");
                Assert.IsNotNull(fileEntry, "Attached file 'doc.txt' must be present in the ZIP");

                using (var s = fileEntry.Open())
                using (var ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    CollectionAssert.AreEqual(payload, ms.ToArray(), "ZIP file content must match the original bytes");
                }
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void File_IsNotIncludedInZip_WhenBytesAreUnavailable()
        {
            var fileId = Guid.NewGuid();
            var messages = MessageBuilder.Sequence(
                MessageBuilder.File("alice", "missing.txt", fileId));

            var zip = RunExport("general", messages, _ => null); // download returns null

            using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
            {
                bool hasFile = archive.Entries.Any(e => e.Name == "missing.txt");
                Assert.IsFalse(hasFile, "File whose bytes were unavailable must not appear in the ZIP");
            }
        }

        // ── Duplicate file names ───────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        public void DuplicateFileName_IsDeduplicatedInZip()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var messages = MessageBuilder.Sequence(
                MessageBuilder.File("alice", "report.txt", id1),
                MessageBuilder.File("bob", "report.txt", id2));

            var zip = RunExport("general", messages, id => new byte[] { 1 });

            using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
            {
                var fileEntries = archive.Entries.Where(e => e.Name != "general channel chat.txt").ToList();
                Assert.AreEqual(2, fileEntries.Count, "Both files with duplicate names must be present");
                var names = fileEntries.Select(e => e.Name).ToList();
                CollectionAssert.AllItemsAreUnique(names, "Duplicate file names must be made unique");
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void DuplicateFileName_UsesParenthesisCounter()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var messages = MessageBuilder.Sequence(
                MessageBuilder.File("alice", "notes.txt", id1),
                MessageBuilder.File("bob", "notes.txt", id2));

            var zip = RunExport("general", messages, id => new byte[] { 1 });

            using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
            {
                var fileNames = archive.Entries
                    .Where(e => !e.Name.EndsWith("channel chat.txt"))
                    .Select(e => e.Name)
                    .ToList();

                Assert.IsTrue(fileNames.Contains("notes.txt"), "First file keeps original name");
                Assert.IsTrue(fileNames.Any(n => n == "notes (1).txt"), "Second duplicate gets ' (1)' suffix");
            }
        }

        // ── Path traversal safety ─────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        public void PathTraversalFileName_IsSanitized()
        {
            var fileId = Guid.NewGuid();
            // Simulated message where content has a path traversal attempt via file name
            var msg = new Message
            {
                SenderId = "attacker",
                Content = "Shared file: ../../evil.txt",
                Type = MessageType.File,
                FileId = fileId,
                Timestamp = DateTime.UtcNow,
                IsCurrentUser = false
            };

            var zip = RunExport("general", MessageBuilder.Wrap(msg), id => new byte[] { 1 });

            using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
            {
                var fileEntry = archive.Entries.FirstOrDefault(e => !e.Name.EndsWith("channel chat.txt"));
                Assert.IsNotNull(fileEntry, "Sanitized file must still appear in ZIP");
                Assert.IsFalse(fileEntry.Name.Contains(".."), "Path traversal must be stripped from entry name");
                Assert.AreEqual("evil.txt", fileEntry.Name, "Only the file name portion must remain");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private byte[] RunExport(string channelName, IEnumerable<Chat.Client.Shared.ViewModels.ConversationItemViewModel> items, Func<Guid, byte[]> download)
        {
            var zipPath = Path.Combine(_tempDir, $"{channelName}.zip");
            _service.ExportChannelChat(zipPath, channelName, items, download);
            return File.ReadAllBytes(zipPath);
        }

        private static string ReadTranscriptFromZip(byte[] zipBytes)
        {
            using (var archive = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read))
            {
                var txt = archive.Entries.FirstOrDefault(e => e.Name.EndsWith("channel chat.txt"));
                Assert.IsNotNull(txt, "ZIP must contain a .txt transcript entry");
                using (var reader = new StreamReader(txt.Open(), Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
