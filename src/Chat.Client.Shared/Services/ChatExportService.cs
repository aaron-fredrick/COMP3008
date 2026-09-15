using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;
using Chat.Client.Shared.ViewModels;

namespace Chat.Client.Shared.Services
{
    public class ChatExportService
    {
        public void ExportChannelChat(string zipFilePath, string channelName, IEnumerable<ConversationItemViewModel> items, Func<Guid, byte[]> downloadFileBytes)
        {
            using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var transcriptBuilder = new StringBuilder();

                foreach (var item in items)
                {
                    if (item is SystemMessageViewModel sysMsg)
                    {
                        string timestamp = sysMsg.Timestamp.ToString("dd/MM/yyyy, HH:mm");
                        transcriptBuilder.AppendLine($"{timestamp} - {sysMsg.Text}");
                    }
                    else if (item is MessageViewModel msgVm)
                    {
                        var message = msgVm.Message;
                        string timestamp = message.Timestamp.ToLocalTime().ToString("dd/MM/yyyy, HH:mm");
                        string senderId = message.SenderId;
                        string content = message.Content;

                        bool isFile = message.Type == MessageType.File || message.FileId.HasValue;
                        if (isFile)
                        {
                            transcriptBuilder.AppendLine($"{timestamp} - {senderId}: <File omitted>");

                            if (message.FileId.HasValue)
                            {
                                string originalFileName = !string.IsNullOrEmpty(content) && content.StartsWith("Shared file: ")
                                    ? content.Substring("Shared file: ".Length)
                                    : $"file_{message.FileId.Value}.dat";

                                // Safely handle file name to prevent path traversal
                                originalFileName = Path.GetFileName(originalFileName);

                                string uniqueFileName = GetUniqueFileName(originalFileName, usedFileNames);
                                usedFileNames.Add(uniqueFileName);

                                byte[] fileBytes = downloadFileBytes(message.FileId.Value);
                                if (fileBytes != null)
                                {
                                    var fileEntry = archive.CreateEntry(uniqueFileName);
                                    using (var fileEntryStream = fileEntry.Open())
                                    {
                                        fileEntryStream.Write(fileBytes, 0, fileBytes.Length);
                                    }
                                }
                            }
                        }
                        else
                        {
                            transcriptBuilder.AppendLine($"{timestamp} - {senderId}: {content}");
                        }
                    }
                }

                // Create the transcript text file last
                var txtEntry = archive.CreateEntry($"{channelName} channel chat.txt");
                using (var entryStream = txtEntry.Open())
                using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                {
                    writer.Write(transcriptBuilder.ToString());
                }
            }
        }

        private string GetUniqueFileName(string fileName, HashSet<string> usedFileNames)
        {
            if (!usedFileNames.Contains(fileName))
            {
                return fileName;
            }

            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            int counter = 1;

            while (true)
            {
                string newFileName = $"{nameWithoutExt} ({counter}){extension}";
                if (!usedFileNames.Contains(newFileName))
                {
                    return newFileName;
                }
                counter++;
            }
        }
    }
}
