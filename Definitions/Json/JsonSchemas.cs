using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace RoflanArchives.Core.Definitions.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
#pragma warning disable IDE0051 // Remove unused private member

public class DefinitionSchema : JsonBaseSchema
{
    private ObservableCollection<ArchiveSchema> _archives =
        new ObservableCollection<ArchiveSchema>
        {

        };
    [JsonPropertyName("archives")]
    [JsonRequired]
    public ObservableCollection<ArchiveSchema> Archives
    {
        get
        {
            return _archives;
        }
        set
        {
            _archives = value;
            OnPropertyChanged();
        }
    }
}

public class ArchiveSchema : JsonBaseSchema
{
    private string _filePath = string.Empty;
    [JsonPropertyName("file-path")]
    [JsonRequired]
    public string FilePath
    {
        get
        {
            return _filePath;
        }
        set
        {
            _filePath = value;
            OnPropertyChanged();
        }
    }
    private string? _name = null;
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Name
    {
        get
        {
            return _name;
        }
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }
    private RoflanArchiveCompressionType _compressionType = RoflanArchiveCompressionType.Default;
    [JsonPropertyName("compression-type")]
    [JsonConverter(typeof(JsonStringEnumConverter<RoflanArchiveCompressionType>))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public RoflanArchiveCompressionType CompressionType
    {
        get
        {
            return _compressionType;
        }
        set
        {
            _compressionType = value;
            OnPropertyChanged();
        }
    }
    private long? _compressionLevel = null;
    [JsonPropertyName("compression-level")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long? CompressionLevel
    {
        get
        {
            return _compressionLevel;
        }
        set
        {
            _compressionLevel = value;
            OnPropertyChanged();
        }
    }
    private string? _version = null;
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Version
    {
        get
        {
            return _version;
        }
        set
        {
            _version = value;
            OnPropertyChanged();
        }
    }
    private ObservableCollection<SourceDirectorySchema> _sourceDirectories =
        new ObservableCollection<SourceDirectorySchema>
        {

        };
    [JsonPropertyName("source-directories")]
    [JsonRequired]
    public ObservableCollection<SourceDirectorySchema> SourceDirectories
    {
        get
        {
            return _sourceDirectories;
        }
        set
        {
            _sourceDirectories = value;
            OnPropertyChanged();
        }
    }
}

public class SourceDirectorySchema : JsonBaseSchema
{
    private string _path = string.Empty;
    [JsonPropertyName("path")]
    [JsonRequired]
    public string Path
    {
        get
        {
            return _path;
        }
        set
        {
            _path = value;
            OnPropertyChanged();
        }
    }
    private ObservableCollection<FileSchema>? _files = null;
    [JsonPropertyName("files")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ObservableCollection<FileSchema>? Files
    {
        get
        {
            return _files;
        }
        set
        {
            _files = value;
            OnPropertyChanged();
        }
    }
    private ObservableCollection<string>? _blacklistPaths = null;
    [JsonPropertyName("blacklist-paths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ObservableCollection<string>? BlacklistPaths
    {
        get
        {
            return _blacklistPaths;
        }
        set
        {
            _blacklistPaths = value;
            OnPropertyChanged();
        }
    }
    private int? _maxDepth = null;
    [JsonPropertyName("max-depth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? MaxDepth
    {
        get
        {
            return _maxDepth;
        }
        set
        {
            _maxDepth = value;
            OnPropertyChanged();
        }
    }
}

public class FileSchema : JsonBaseSchema
{
    private string _path = string.Empty;
    [JsonPropertyName("path")]
    [JsonRequired]
    public string Path
    {
        get
        {
            return _path;
        }
        set
        {
            _path = value;
            OnPropertyChanged();
        }
    }
    private uint? _id = null;
    [JsonPropertyName("id")]
    public uint? Id
    {
        get
        {
            return _id;
        }
        set
        {
            _id = value;
            OnPropertyChanged();
        }
    }
    private RoflanArchiveCompressionType _compressionType = RoflanArchiveCompressionType.Default;
    [JsonPropertyName("compression-type")]
    [JsonConverter(typeof(JsonStringEnumConverter<RoflanArchiveCompressionType>))]
    public RoflanArchiveCompressionType CompressionType
    {
        get
        {
            return _compressionType;
        }
        set
        {
            _compressionType = value;
            OnPropertyChanged();
        }
    }
    private long? _compressionLevel = null;
    [JsonPropertyName("compression-level")]
    public long? CompressionLevel
    {
        get
        {
            return _compressionLevel;
        }
        set
        {
            _compressionLevel = value;
            OnPropertyChanged();
        }
    }
}

#pragma warning restore IDE0051 // Remove unused private member
// ReSharper restore UnusedMember.Global
// ReSharper restore ClassNeverInstantiated.Global
