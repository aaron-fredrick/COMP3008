using System;

namespace Chat.Server.FileStorage
{
    /// <summary>
    /// Stores and retrieves file content independently from file metadata.
    /// The server keeps metadata in memory while this abstraction owns the content bytes.
    /// </summary>
    public interface IFileContentStore
    {
        string Store(Guid fileId, byte[] content);
        byte[] Read(Guid fileId);
        System.IO.Stream OpenRead(Guid fileId);
        bool Exists(Guid fileId);
        void Delete(Guid fileId);
    }
}
