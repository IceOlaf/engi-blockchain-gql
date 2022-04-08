using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Engi.Substrate.Metadata.V11;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

using MetadataV11Parser = Engi.Substrate.Metadata.V11.MetadataParser;

namespace Engi.Substrate
{
    public class ParserTests
    {
        [Fact]
        public void MetadataV11()
        {
            string expected = JObject.Parse(File.ReadAllText("./TestData/metadata_v11.json")).ToString();

            string metadataString = File.ReadAllText("./TestData/metadata_v11.hex");

            byte[] metadataData = Convert.FromHexString(metadataString.Substring(2));

            var metadata = MetadataV11Parser.Parse(new ScaleStream(metadataData));

            var serializer = new JsonSerializer
            {
                ContractResolver = new CustomContractResolver(),
                Converters =
                {
                    new ByteArrayConverter(),
                    new StringEnumConverter(),
                    new StorageEntryConverter()
                }
            };

            string actual = JObject.FromObject(new
            {
                metadata.MagicNumber,
                metadata = new 
                {
                    v11 = new
                    {
                        metadata.Modules,
                        metadata.Extrinsic
                    }
                }
            }, serializer).ToString();

            Assert.Equal(expected, actual);
        }

        class CustomContractResolver : CamelCasePropertyNamesContractResolver
        {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            {
                var members = base.GetSerializableMembers(objectType);

                members.RemoveAll(x => x.Name == nameof(StorageEntryMetadata.TyType));

                return members;
            }
        }

        class ByteArrayConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue("0x" + Convert.ToHexString((byte[])value));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

            public override bool CanConvert(Type objectType) => objectType == typeof(byte[]);
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
                        writer.WriteValue(plain.Value);

                        writer.WriteEndObject();
                        break;

                    case StorageEntryMap map:
                        writer.WriteStartObject();

                        writer.WritePropertyName("map");
                    
                        writer.WriteStartObject();
                    
                        writer.WritePropertyName("hasher");
                        writer.WriteValue(map.Hasher.ToString());

                        writer.WritePropertyName("key");
                        writer.WriteValue(map.Key);

                        writer.WritePropertyName("value");
                        writer.WriteValue(map.Value);

                        writer.WritePropertyName("linked");
                        writer.WriteValue(map.Linked);

                        writer.WriteEndObject();

                        writer.WriteEndObject();
                        break;

                    case StorageEntryDoubleMap doubleMap:
                        writer.WriteStartObject();

                        writer.WritePropertyName("doubleMap");

                        writer.WriteStartObject();

                        writer.WritePropertyName("hasher");
                        writer.WriteValue(doubleMap.Hasher.ToString());

                        writer.WritePropertyName("key1");
                        writer.WriteValue(doubleMap.Key1);

                        writer.WritePropertyName("key2");
                        writer.WriteValue(doubleMap.Key2);

                        writer.WritePropertyName("value");
                        writer.WriteValue(doubleMap.Value);

                        writer.WritePropertyName("key2Hasher");
                        writer.WriteValue(doubleMap.Key2Hasher.ToString());

                        writer.WriteEndObject();

                        writer.WriteEndObject();
                        break;

                    default: throw new NotImplementedException(value.GetType().ToString());
                }
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

            public override bool CanConvert(Type objectType) => typeof(IStorageEntry).IsAssignableFrom(objectType);
        }
    }
}
