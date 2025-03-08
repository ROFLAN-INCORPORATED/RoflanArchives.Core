using System;
using System.Text.Json.Serialization;

namespace RoflanArchives.Core.Definitions.Json;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(DefinitionSchema))]
public partial class DefinitionSchemaContext : JsonSerializerContext
{

}
