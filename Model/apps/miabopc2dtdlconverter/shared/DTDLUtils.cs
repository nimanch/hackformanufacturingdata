using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MiaB.model.opc2dtdl
{
    public class DTDLInterface {
        public DTDLInterface() {
            Type = "Interface";
            Extends = new List<string>();
            Contents = new List<DTDLContentItem>();
        }

        [JsonPropertyName("@context")]
        public string Context { get{return "dtmi:dtdl:context;2";} }

        [JsonPropertyName("@type")]
        public string Type {get; }
        [JsonPropertyName("@id")]
        public string Id {get; set;}

        public List<string> Extends {get; set;}

        public List<DTDLContentItem> Contents {get; set;}

    }

    public class DTDLContentItem {
        [JsonPropertyName("@type")]
        public string Type {get; set;}
    }

    public class DTDLPropertyItem: DTDLContentItem {
        public DTDLPropertyItem() {
            Type = "Property";
        }
        public string Name {get; set;}
        public string Schema {get;set;}
    }

    public class DTDLComponentItem: DTDLContentItem {
        public DTDLComponentItem() {
            Type = "Component";
        }
        public string Name {get; set;}
        public string Schema {get;set;}
    }

    public class DTDLEnumPropertyItem: DTDLContentItem {
        public DTDLEnumPropertyItem() {
            Type = "Property";
        }
        public string Name {get; set;}
        public DTDLEnumSchema Schema {get;set;}
    }

    public class DTDLEnumSchema {

        public DTDLEnumSchema() {
            Type = "Enum";
            ValueSchema = "integer";
            EnumValues = new List<DTDLEnumItem>();
        }

        [JsonPropertyName("@type")]
        public string Type {get; set;}

        private string _valueSchema;
        public string ValueSchema {
            get{ return _valueSchema;} 
            set{  
                if (value == "integer" )
                    _valueSchema = value;
                else throw new System.Exception("DTDLEnum: value schema must be integer");
            }
        }

        public List<DTDLEnumItem> EnumValues {get;set;}
    }

    public class DTDLEnumItem {
        public string Name {get; set;} 
        public int EnumValue {get;set;} 
    }

}