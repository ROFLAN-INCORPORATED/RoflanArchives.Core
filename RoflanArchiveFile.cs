using System;
using System.IO;

namespace RoflanArchives.Core;

public class RoflanArchiveFile : IRoflanArchiveFile
{
    uint IRoflanArchiveFileDefinition.Id { get; set; }
    string IRoflanArchiveFileDefinition.RelativePath { get; set; }
    RoflanArchiveCompressionType IRoflanArchiveFileDefinition.CompressionType { get; set; }
    byte IRoflanArchiveFileDefinition.CompressionLevel { get; set; }
    ReadOnlyMemory<byte> IRoflanArchiveFileDefinition.ContentHash { get; set; }
    ulong IRoflanArchiveFileDefinition.OriginalContentSize { get; set; }
    ulong IRoflanArchiveFileDefinition.ContentSize { get; set; }
    ulong IRoflanArchiveFileDefinition.ContentOffset { get; set; }

    // Runtime Only Properties
    string IRoflanArchiveFileDefinition.Name { get; set; }
    string IRoflanArchiveFileDefinition.Extension { get; set; }
    string IRoflanArchiveFileDefinition.DirectoryPath { get; set; }
    ulong IRoflanArchiveFileDefinition.EndOffset { get; set; }
    //


    ReadOnlyMemory<byte> IRoflanArchiveFileContent.Data { get; set; }
    Stream IRoflanArchiveFileContent.DataStream { get; set; }



    public uint Id
    {
        get
        {
            return ((IRoflanArchiveFile)this).Id;
        }
    }
    public string RelativePath
    {
        get
        {
            return ((IRoflanArchiveFile)this).RelativePath;
        }
    }
    public string Name
    {
        get
        {
            return ((IRoflanArchiveFile)this).Name;
        }
    }
    public string Extension
    {
        get
        {
            return ((IRoflanArchiveFile)this).Extension;
        }
    }
    [Obsolete($"For compatibility with API versions lower than 1.5.0.0. Use {nameof(DataStream)} property instead")]
    public ReadOnlyMemory<byte> Data
    {
        get
        {
            return ((IRoflanArchiveFile)this).Data;
        }
    }
    public Stream DataStream
    {
        get
        {
            return ((IRoflanArchiveFile)this).DataStream;
        }
    }

    internal string DirectoryPath
    {
        get
        {
            return ((IRoflanArchiveFile)this).DirectoryPath;
        }

    }



#pragma warning disable CS8618

    internal RoflanArchiveFile(
        uint id,
        string relativePath,
        RoflanArchiveCompressionType compressionType = RoflanArchiveCompressionType.Inherited,
        byte? compressionLevel = null,
        ulong size = 0,
        ulong offset = 0)
    {
        var definition = (IRoflanArchiveFileDefinition)this;

        definition.Id = id;
        definition.RelativePath = relativePath;
        definition.CompressionType = compressionType;
        definition.CompressionLevel = compressionLevel ?? 0;
        definition.ContentHash = new byte[8];
        definition.ContentSize = size;
        definition.ContentOffset = offset;

        definition.Name = System.IO.Path.GetFileNameWithoutExtension(RelativePath);
        definition.Extension = System.IO.Path.GetExtension(RelativePath);

        var content = (IRoflanArchiveFileContent)this;

#pragma warning disable CS0618 // Тип или член устарел
        content.Data = ReadOnlyMemory<byte>.Empty;
#pragma warning restore CS0618 // Тип или член устарел
        content.DataStream = new MemoryStream();
    }

#pragma warning restore CS8618



    public FileStream GetReadStream()
    {
        var filePath = System.IO.Path.Combine(DirectoryPath, RelativePath);

        return File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public FileStream GetWriteStream()
    {
        var filePath = System.IO.Path.Combine(DirectoryPath, RelativePath);

        return File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
    }
}
