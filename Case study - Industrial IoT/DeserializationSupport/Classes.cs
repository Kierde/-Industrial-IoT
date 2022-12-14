using System.Text.Json;
using Newtonsoft.Json; 

namespace DeserializationClasses
{

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
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

        [JsonProperty("$version")]
        public int version { get; set; }
    }

    public class Root
    {
        public object deviceId { get; set; }
        public object etag { get; set; }
        public object version { get; set; }
        public Properties properties { get; set; }
    }


}