# <div align="center">**RoflanArchives.Core**</div>
## <div align="center">[![CodeLines](https://tokei.rs/b1/github/ROFLAN-INCORPORATED/RoflanArchives.Core?category=code)](https://github.com/ROFLAN-INCORPORATED/RoflanArchives.Core) [![ClosedMilestones](https://img.shields.io/github/milestones/closed/ROFLAN-INCORPORATED/RoflanArchives.Core?style=flat)](https://github.com/ROFLAN-INCORPORATED/RoflanArchives.Core/milestones?state=closed) [![ClosedIssues](https://img.shields.io/github/issues-closed/ROFLAN-INCORPORATED/RoflanArchives.Core?style=flat)](https://github.com/ROFLAN-INCORPORATED/RoflanArchives.Core/issues?q=is%3Aissue+is%3Aclosed)</div>

### NuGet Packages

- **[RoflanArchives.Core](https://www.nuget.org/packages/RoflanArchives.Core)**<br/>
    [![Nuget](https://img.shields.io/nuget/v/RoflanArchives.Core?style=flat)](https://www.nuget.org/packages/RoflanArchives.Core)
    [![Nuget](https://img.shields.io/nuget/dt/RoflanArchives.Core?style=flat)](https://www.nuget.org/packages/RoflanArchives.Core)

Core library for working with roflan engine archive files (\*.roflarc)

<br/>

# <div align="center">**Code Examples**</div>

## Pack a directory to the archive

```csharp
var directoryPath = Console.ReadLine();
var fileName = Console.ReadLine();
var sourceDirectoryPath = Console.ReadLine();
var compressionType = Convert.ToByte(Console.ReadLine());
var compressionLevel = Convert.ToByte(Console.ReadLine());

var compressionTypeEnum = RoflanArchiveCompressionType.Default;

if (Enum.IsDefined(typeof(RoflanArchiveCompressionType), compressionType))
    compressionTypeEnum = (RoflanArchiveCompressionType)compressionType;

var sources = new[]
{
    new RoflanArchiveSourceDirectoryInfo(sourceDirectoryPath)
}

RoflanArchive.Pack(directoryPath, fileName, sources,
    compressionType, compressionLevel);
```

## Open and load an existed archive

```csharp
var filePath = Console.ReadLine();

var archive = RoflanArchive.Open(
    filePath);
```

## Loading only one file from the archive (*by id*)

```csharp
var filePath = Console.ReadLine();
var id = Convert.ToUInt32(Console.ReadLine());

var file = RoflanArchive.GetFile(
    filePath, id);
```

## Loading only one file from the archive (*by relative path*)

```csharp
var filePath = Console.ReadLine();
var relativePath = Console.ReadLine();

var file = RoflanArchive.GetFile(
    filePath, relativePath);
```

## Unpack the archive to a directory

```csharp
var filePath = Console.ReadLine();
var targetDirectoryPath = Console.ReadLine();

RoflanArchive.Unpack(filePath,
    targetDirectoryPath);
```
