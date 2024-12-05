using System;

using Newtonsoft.Json;

namespace KLPlugins.DynLeaderboards.Common;

internal class NotSerializableException(string msg) : NotSupportedException(msg) { }

internal class NotDeserializableException(string msg) : NotSupportedException(msg) { }

internal class FailJsonConverter : JsonConverter {
    protected string SerializeMsg { get; set; } = "";
    protected string DeserializeMsg { get; set; } = "";

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
        throw new NotSerializableException(this.SerializeMsg);
    }

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer
    ) {
        throw new NotDeserializableException(this.DeserializeMsg);
    }

    public override bool CanRead => true;
    public override bool CanWrite => true;

    public override bool CanConvert(Type objectType) {
        return true;
    }
}