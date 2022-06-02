# <div align="center">**RoflanArchive.Core**</div>

Core library for working with roflan engine archive files (\*.roflarc)

<br/>

# <div align="center">**Code Examples**</div>

## Pack a directory to the archive

```csharp
var directoryPath = Console.ReadLine();
var fileName = Console.ReadLine();
var sourceDirectoryPath = Console.ReadLine();
var compressionLevel = Convert.ToInt32(Console.ReadLine());

if (compressionLevel is < (int)LZ4Level.L00_FAST or > (int)LZ4Level.L12_MAX)
    compressionLevel = (int)LZ4Level.L00_FAST;

RoflanArchiveFile.Pack(directoryPath,
    fileName, sourceDirectoryPath,
    (LZ4Level)compressionLevel);
```

## Open and load an existed archive

```csharp
var filePath = Console.ReadLine();

var archive = RoflanArchiveFile.Open(
    filePath);
```

## Loading only one file from the archive (*by id*)

```csharp
var filePath = Console.ReadLine();
var id = Convert.ToUInt32(Console.ReadLine());

var file = RoflanArchiveFile.GetFile(
    filePath, id);
```

## Loading only one file from the archive (*by relative path*)

```csharp
var filePath = Console.ReadLine();
var relativePath = Console.ReadLine();

var file = RoflanArchiveFile.GetFile(
    filePath, relativePath);
```

## Unpack the archive to a directory

```csharp
var filePath = Console.ReadLine();
var targetDirectoryPath = Console.ReadLine();

RoflanArchiveFile.Unpack(filePath,
    targetDirectoryPath);
```
