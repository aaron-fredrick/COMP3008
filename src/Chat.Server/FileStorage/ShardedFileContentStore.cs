using System;
using System.IO;

namespace Chat.Server.FileStorage
{
    /// <summary>
    /// Filesystem-backed content store using a two-level hexadecimal prefix shard.
    /// Example: 48f574... -> StoredFiles/48/f5/48f574....blob
    /// </summary>
    public sealed class ShardedFileContentStore : IFileContentStore
    {
        private readonly string _rootDirectory;

        public ShardedFileContentStore(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new ArgumentException("A storage directory is required.", nameof(rootDirectory));

            _rootDirectory = rootDirectory;
            Directory.CreateDirectory(_rootDirectory);
        }

        public string Store(Guid fileId, byte[] content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            string key = fileId.ToString("N");
            string path = GetPath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, content);
            return key;
        }

        public byte[] Read(Guid fileId)
        {
            string path = GetPath(fileId.ToString("N"));
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        public bool Exists(Guid fileId)
        {
            return File.Exists(GetPath(fileId.ToString("N")));
        }

        public void Delete(Guid fileId)
        {
            string path = GetPath(fileId.ToString("N"));
            if (File.Exists(path)) File.Delete(path);
        }

        private string GetPath(string storageKey)
        {
            if (storageKey.Length < 4) throw new ArgumentException("Storage key is too short.", nameof(storageKey));

            string firstShard = storageKey.Substring(0, 2).ToLowerInvariant();
            string secondShard = storageKey.Substring(2, 2).ToLowerInvariant();
            return Path.Combine(_rootDirectory, firstShard, secondShard, storageKey + ".blob");
        }
    }
}
