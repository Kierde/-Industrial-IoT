using System.Text.Json;
using Newtonsoft.Json; 

namespace DeserializationClasses
{
    public class Desired
    {
        public List<int> ProductionRate { get; set; }

        [JsonProperty("$version")]
        public int version { get; set; }
    }

    public class Properties
    {
        public Desired desired { get; set; }
        public Reported reported { get; set; }
    }

    public class Reported
    {
        public List<int> ProductionRate { get; set; }
        public List<string> DevicesErrors { get; set; }
        public List<string> DateOfLastMaintenance { get; set; }
        public List<string> LastErrorDate { get; set; }

        [JsonProperty("$version")]
        public int version { get; set; }
    }

    public class DeviceTwin
    {
        public object deviceId { get; set; }
        public object etag { get; set; }
        public object version { get; set; }
        public Properties properties { get; set; }
    }

    public class TeleValueMachine
    {
        [JsonIgnore]
        public string id_Of_Machine { get; set; }

        [JsonIgnore]
        public int production_rate { get; set; }
        [JsonIgnore]
        public int device_error { get; set; }

        public int production_status { get; set; }
        public string workorder_id { get; set; }
        public int good_count { get; set; }
        public int bad_count { get; set; }
        public double temperature { get; set; }
    }

    public class ErrorMessage
    {
        public ErrorMessage(string id_Of_Machine, int device_error)
        {
            this.id_Of_Machine = id_Of_Machine;
            this.device_error = device_error;
        }

        public string id_Of_Machine { get; set; }
        public int device_error { get; set; }
    }

    public class ConfigJsonFile
    {
        public string iot_connection_string {get; set;}
        public string opc_server_adress {get; set;}
    }



}