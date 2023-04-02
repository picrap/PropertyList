# PropertyList

A plist (short term for “property list”) reader and writer.

## Package

Available as a [![NuGet](https://img.shields.io/nuget/v/PropertyList.svg?style=flat-square)](https://www.nuget.org/packages/PropertyList) package.

## How to use it

### Supported types

The library handles the following entities:

| plist type | write | read |
|--|--|--|
| `dict` | `IDictionary<string,object>`, `IReadOnlyDictionary<string,object>` | `Dictionary<string,object>` (where `object`s are any other type) |
| `array` | `IList` | `List<object>` (where `object`s are any other type) |
| `string` | `string` | `string` |
| `real` | `float`, `double`, `decimal` | `decimal` |
| `integer` | `byte`, `sbyte`, `int`, `uint`, `long`, `ulong`  | `long` |
| `date` | | `DateTimeOffset` |
| `true`/`false` | | `bool` |

### Reading

```csharp
using var reader = File.Open("/tmp/my.plist");
var plistReader = new PlistReader();
var plist = plistReader.Read(reader);
var label = (string)plist["Label"];
var programArguments = (IList)plist["ProgramArguments"];
```

### Writing

```csharp
var plist = new Dictionary<string, object>
{
    {"Label", "here"},
    {"LaunchOnlyOnce", true},
};
using var writer = File.Create("/tmp/my.plist");
var plistWriter = new PlistWriter();
plistWriter.Write(plist, writer);
```
