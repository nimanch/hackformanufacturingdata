using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Opc.Ua.Export;

namespace MiaB.model.opc2dtdl
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // Read OPC-UA NodesSet xml file and parse it into a UANodeSet object
            string nodesetxmlfilepath = "..\\..\\assets\\miab10-nodeset2.xml";

            Stream stream = new FileStream(nodesetxmlfilepath, FileMode.Open);
            Opc.Ua.Export.UANodeSet nodeSet = Opc.Ua.Export.UANodeSet.Read(stream);

            Dictionary<string, UANode> nodeDict = new Dictionary<string, UANode>();
            foreach (var item in nodeSet.Items)
            {
                nodeDict.Add(item.NodeId, item);
            }

            if (nodeDict.ContainsKey("ns=2;s=Beijer.nsuri=TagProvider;s=Tags"))
            {
                UAObject tagsObject = nodeDict["ns=2;s=Beijer.nsuri=TagProvider;s=Tags"] as UAObject;

                DTDLInterface di = new DTDLInterface();
                di.Extends = null;
                di.Id = "dtmi:opcfoundation:org:UA:MiaB:Tags;1";

                var varRefs = Array.FindAll(tagsObject.References,
                    varNodeRef => varNodeRef.ReferenceType == "HasComponent" && varNodeRef.IsForward);
                foreach (var item in varRefs)
                {
                    UAVariable uaVar = nodeDict[item.Value] as UAVariable;
                    DTDLPropertyItem dpi = new DTDLPropertyItem();
                    string[] nodeIdParts = uaVar.NodeId.Split(';');
                    dpi.Name = nodeIdParts[2].Substring(2);
                    dpi.Schema = "integer";
                    di.Contents.Add(dpi);
                }

                Console.WriteLine("Hello World!");
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    IgnoreNullValues = true,
                };
                // This is a workaround for the lack of polymorphic serialization in System.Text.Json
                // The suggested workaround in the docs (use object to declare the child item) did not work for me
                // But this custom serializer does
                options.Converters.Add(new HeterogenousListConverter<DTDLContentItem, List<DTDLContentItem>>());
                string ts = JsonSerializer.Serialize(di, options);
                System.IO.File.WriteAllText(".\\dtmi_opcfoundation_org_UA_MiaB-Tags-interface-1.json", ts);
            }
        }
    }

    // https://stackoverflow.com/questions/59743382/is-there-a-simple-way-to-manually-serialize-deserialize-child-objects-in-a-custo/59744188
    // Thrown out everything for deserialization, as we won't try to do that
    // This is a workaround for the lack of polymorphic serialization in System.Text.Json
    // Docs say that you can work around this by declaring property as object
    // but that did not work - this does.
    // Not pretty, but...
    public class HeterogenousListConverter<TItem, TList> : JsonConverter<TList>
    where TItem : notnull
    where TList : IList<TItem>, new()
    {

        public HeterogenousListConverter(params (string key, Type type)[] mappings)
        {
        }

        public override TList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {

            // Helper function for validating where you are in the JSON    
            void validateToken(Utf8JsonReader reader, JsonTokenType tokenType)
            {
                if (reader.TokenType != tokenType)
                    throw new JsonException($"Invalid token: Was expecting a '{tokenType}' token but received a '{reader.TokenType}' token");
            }

            validateToken(reader, JsonTokenType.StartArray);

            var results = new TList();

            reader.Read(); // Advance to the first object after the StartArray token. This should be either a StartObject token, or the EndArray token. Anything else is invalid.

            while (reader.TokenType == JsonTokenType.StartObject)
            { // Start of 'wrapper' object

                reader.Read(); // Move to property name
                validateToken(reader, JsonTokenType.PropertyName);

                reader.Read(); // Move to start of object (stored in this property)
                validateToken(reader, JsonTokenType.StartObject); // Start of vehicle

                reader.Read(); // Move past end of item object
                reader.Read(); // Move past end of 'wrapper' object
            }

            validateToken(reader, JsonTokenType.EndArray);

            return results;
        }

        public override void Write(Utf8JsonWriter writer, TList items, JsonSerializerOptions options)
        {

            writer.WriteStartArray();

            foreach (var item in items)
            {

                var itemType = item.GetType();

                //writer.WriteStartObject();

                JsonSerializer.Serialize(writer, item, itemType, options);

                //writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
}
