using Newtonsoft.Json;
using System;
using VolumeControl.LocalizationUtil.ViewModels;

namespace VolumeControl.LocalizationUtil.JsonConverters
{
    public class JsonNodeJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType.IsAssignableTo(typeof(JsonNodeVM));
        public override bool CanRead => false;
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => throw new NotImplementedException();
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
                writer.WriteNull();
            else if (value is JsonObjectVM objectVM)
            {
                objectVM.ToJObject().WriteTo(writer, this);
            }
            else if (value is JsonValueVM valueVM)
            {
                valueVM.ToJProperty().WriteTo(writer, this);
            }
            else throw new InvalidOperationException($"Unexpected type \"{value.GetType()}\" cannot be serialized!");
        }
    }
}
