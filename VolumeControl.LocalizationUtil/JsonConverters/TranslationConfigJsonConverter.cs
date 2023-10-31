using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using VolumeControl.LocalizationUtil.ViewModels;

namespace VolumeControl.LocalizationUtil.JsonConverters
{
    public class TranslationConfigJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType.IsAssignableFrom(typeof(TranslationConfigVM));
        public override bool CanRead => false;
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => throw new NotImplementedException();
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
                writer.WriteNull();
            else if (value is TranslationConfigVM configVM)
            {
                JObject rootObject = new()
                { // insert the language name as the first object
                    { "LanguageName", new JObject() { new JProperty(configVM.LanguageName, configVM.LanguageDisplayName) } }
                };

                // merge the root node
                rootObject.Merge(configVM.RootNode.ToJObject());

                // serialize the root node copy
                rootObject.WriteTo(writer, new JsonNodeJsonConverter());
            }
            else throw new InvalidOperationException($"Translation config type \"{value.GetType()}\" is not supported!");
        }
    }
}
