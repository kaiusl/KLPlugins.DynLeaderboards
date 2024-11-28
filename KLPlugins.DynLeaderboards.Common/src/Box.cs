using System;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.Common;

public sealed class Box<T>(T value)
    where T : struct {
    public T Value = value;
}


public sealed class BoxJsonConverter<T> : JsonConverter
    where T : struct {
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
        if (value is Box<T> box) {
            writer.WriteValue(box.Value);
        } else {
            throw new ArgumentException("value must be Box<T>");
        }
    }

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer
    ) {
        var t = serializer.Deserialize<T?>(reader);
        if (t == null) {
            return null;
        }

        return new Box<T>(t.Value);
    }

    public override bool CanConvert(Type objectType) {
        return objectType == typeof(T?);
    }
}