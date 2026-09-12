using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chat.Client.Shared.Services;
using Chat.Client.Shared.ViewModels;
using Chat.Client.Shared.Converters;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;
using Chat.Client.Tests.Helpers;

namespace Chat.Client.Tests.Smoke
{
    /// <summary>
    /// Quick sanity checks that verify core client-shared types construct without error.
    /// These do not test behaviour in detail — see Unit tests for that.
    /// </summary>
    [TestClass]
    public class ClientSmokeTests
    {
        [TestMethod]
        [TestCategory("Smoke")]
        public void ChatExportService_Instantiates()
        {
            var svc = new ChatExportService();
            Assert.IsNotNull(svc);
        }

        [TestMethod]
        [TestCategory("Smoke")]
        public void MessageViewModel_Instantiates_FromTextMessage()
        {
            var vm = new MessageViewModel(MessageBuilder.Text("alice", "hi"), showMetadata: true);
            Assert.IsNotNull(vm);
            Assert.AreEqual("alice", vm.SenderId);
        }

        [TestMethod]
        [TestCategory("Smoke")]
        public void Converters_Instantiate()
        {
            Assert.IsNotNull(new FileSizeConverter());
            Assert.IsNotNull(new InitialsConverter());
            Assert.IsNotNull(new IsCurrentUserConverter());
            Assert.IsNotNull(new FileNameExtractorConverter());
        }

        [TestMethod]
        [TestCategory("Smoke")]
        public void ExportService_ProducesValidZip_ForSingleMessage()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "COMP3008-Smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var zipPath = Path.Combine(tempDir, "smoke.zip");
                var svc = new ChatExportService();
                svc.ExportChannelChat(zipPath, "smoke-channel",
                    MessageBuilder.Sequence(MessageBuilder.Text("alice", "smoke test")),
                    _ => null);

                Assert.IsTrue(File.Exists(zipPath), "Export must produce a ZIP file");
                using (var archive = new ZipArchive(File.OpenRead(zipPath)))
                {
                    Assert.IsTrue(archive.Entries.Count > 0, "ZIP must not be empty");
                }
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }
    }
}
