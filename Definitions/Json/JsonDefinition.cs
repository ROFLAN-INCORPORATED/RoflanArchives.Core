using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;

namespace RoflanArchives.Core.Definitions.Json;

public class JsonDefinition
{
    public DefinitionSchema Schema { get; }



    public JsonDefinition(Stream stream)
    {
        Schema = Read(
            stream);
    }



    private static DefinitionSchema Read(
        Stream stream)
    {
        var schema = JsonSerializer
            .Deserialize<DefinitionSchema>(stream);

        if (schema == null)
            throw new SerializationException("Error when trying to read json definition from stream");

        return schema;
    }
}
