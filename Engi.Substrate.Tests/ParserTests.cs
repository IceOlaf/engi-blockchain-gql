using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Engi.Substrate.Metadata.V14;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

using RuntimeMetadataV14 = Engi.Substrate.Metadata.V14.RuntimeMetadata;

namespace Engi.Substrate
{
    public class ParserTests
    {
        [Fact]
        public void MetadataV14_Types()
        {
            var stream = CreateScaleStreamFromFile("./TestData/metadata_v14.hex");

            var metadata = RuntimeMetadataV14.Parse(stream);

            var actual = metadata.Types;

            AssetJsonEquals("./TestData/metadata_v14_types.json", actual);
        }

        [Fact]
        public void MetadataV14_Metadata()
        {
            var stream = CreateScaleStreamFromFile("./TestData/metadata_v14.hex");

            var metadata = RuntimeMetadataV14.Parse(stream);

            var actual = new
            {
                metadata.MagicNumber,
                metadata = new
                {
                    v14 = new
                    {
                        metadata.Pallets,
                        metadata.Extrinsic,
                        Type = metadata.TypeId
                    }
                }
            };

            AssetJsonEquals("./TestData/metadata_v14.json", actual);
        }

        private static ScaleStream CreateScaleStreamFromFile(string filename)
        {
            string hex = File.ReadAllText(filename);

            byte[] data = Convert.FromHexString(hex.Substring(2));

            return new ScaleStream(data);
        }

        private static void AssetJsonEquals(
            string expectedFilename,
            object? actual)
        {
            string fileJson = File.ReadAllText(expectedFilename);
            string expectedJson = JToken.Parse(fileJson).ToString(Formatting.None);

            var serializer = new JsonSerializer
            {
                ContractResolver = new CustomContractResolver(),
                Converters =
                {
                    new ByteArrayConverter(),
                    new StorageEntryConverter(),
                    new StringEnumConverter(),
                    new TTypeConverter(),
                    new TypePortableFormConverter()
                }
            };

            string actualJson = JToken.FromObject(actual, serializer).ToString(Formatting.None);

            Assert.Equal(expectedJson, actualJson);
        }

        class CustomContractResolver : CamelCasePropertyNamesContractResolver
        {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            {
                var members = base.GetSerializableMembers(objectType);

                if (typeof(TypeDefinition).IsAssignableFrom(objectType))
                {
                    members.RemoveAll(x => x.Name == nameof(TypeDefinition.DefinitionType));
                }

                return members;
            }
        }

        class StorageEntryConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                switch (value)
                {
                    case StorageEntryPlain plain:
                        writer.WriteStartObject();

                        writer.WritePropertyName("plain");
                        writer.WriteValue(plain.Value!.Value);

                        writer.WriteEndObject();
                        break;

                    case StorageEntryMap map:
                        writer.WriteStartObject();

                        writer.WritePropertyName("map");

                        serializer.Serialize(writer, new
                        {
                            map.Hashers,
                            map.Key,
                            map.Value
                        });

                        writer.WriteEndObject();
                        break;

                    default: throw new NotImplementedException(value.GetType().ToString());
                }
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

            public override bool CanConvert(Type objectType)
            {
                return typeof(IStorageEntry).IsAssignableFrom(objectType);
            }
        }

        class TypePortableFormConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter w, object value, JsonSerializer serializer)
            {
                var t = (TypePortableForm)value;

                w.WriteStartObject();

                w.WritePropertyName("path");
                serializer.Serialize(w, t.Path);

                w.WritePropertyName("params");
                serializer.Serialize(w, t.Params);

                w.WritePropertyName("def");
                Write(w, t.Definition!, serializer);

                w.WritePropertyName("docs");
                serializer.Serialize(w, t.Docs);
                
                w.WriteEndObject();
            }

            private void Write(JsonWriter w, TypeDefinition def, JsonSerializer serializer)
            {
                w.WriteStartObject();

                w.WritePropertyName(def.DefinitionType.ToString().ToLowerInvariant());

                if (def is PrimitiveTypeDefinition primitiveType)
                {
                    serializer.Serialize(w, primitiveType.PrimitiveType);
                }
                else
                {
                    serializer.Serialize(w, def);
                }

                w.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

            public override bool CanConvert(Type objectType) => objectType == typeof(TypePortableForm);
        }

        class TTypeConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var t = (TType) value;
                writer.WriteValue(t.Value);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

            public override bool CanConvert(Type objectType) => objectType == typeof(TType);
        }

        class ByteArrayConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue("0x" + Convert.ToHexString((byte[])value).ToLowerInvariant());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

            public override bool CanConvert(Type objectType) => objectType == typeof(byte[]);
        }
    }
}
