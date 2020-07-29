
using Newtonsoft.Json;
using System;

namespace JsonToCsvConvertor.Data
{
    public class MiabData
    {
        public Guid dtId { get; set; }

        public string timestamp { get; set; }

        public string Analog{get;set;}

        public string Humidity { get; set; }

        public string Temperature { get; set; }

        public string Blue { get; set; }

        public string CO2 { get; set; }

        public string Digital { get; set; }

        public override string ToString()
        {
            return this.dtId.ToString() + "," + this.timestamp.ToString() + "," + this.Analog.ToString()+","+ this.Humidity.ToString() + "," + this.Temperature.ToString() + "," + this.Blue.ToString() + "," + this.CO2.ToString() + "," + this.Digital.ToString();
        }
    }
}
