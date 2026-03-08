using System.Text.Json;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace KinkLinkServer.Infrastructure;

public sealed class JsonElementFormatter : IMessagePackFormatter<JsonElement>
{
    public void Serialize(ref MessagePackWriter writer, JsonElement value, MessagePackSerializerOptions options)
    {
        var json = value.GetRawText();
        writer.Write(json);
    }

    public JsonElement Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var json = reader.ReadString();
        return JsonSerializer.Deserialize<JsonElement>(json ?? "{}");
    }
}

public sealed class JsonElementObjectFormatter : IMessagePackFormatter<object>
{
    public void Serialize(ref MessagePackWriter writer, object value, MessagePackSerializerOptions options)
    {
        if (value is JsonElement jsonElement)
        {
            var json = jsonElement.GetRawText();
            writer.Write(json);
        }
        else
        {
            writer.WriteNil();
        }
    }

    public object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return new JsonElement();
        }

        var json = reader.ReadString();
        return JsonSerializer.Deserialize<JsonElement>(json ?? "{}");
    }
}

public sealed class JsonElementObjectResolver : IFormatterResolver
{
    public static readonly JsonElementObjectResolver Instance = new();

    private static readonly JsonElementFormatter JsonFormatter = new();
    private static readonly JsonElementObjectFormatter ObjectFormatter = new();

    public IMessagePackFormatter<T>? GetFormatter<T>()
    {
        var formatterType = typeof(T);

        if (formatterType == typeof(JsonElement))
        {
            return (IMessagePackFormatter<T>)(IMessagePackFormatter<JsonElement>)JsonFormatter;
        }

        if (formatterType == typeof(object))
        {
            return (IMessagePackFormatter<T>)(IMessagePackFormatter<object>)ObjectFormatter;
        }

        return null;
    }
}

public static class MessagePackOptionsExtensions
{
    public static MessagePackSerializerOptions WithJsonElementSupport(this MessagePackSerializerOptions options)
    {
        var resolver = CompositeResolver.Create(
            JsonElementObjectResolver.Instance,
            options.Resolver
        );
        return options.WithResolver(resolver);
    }
}
