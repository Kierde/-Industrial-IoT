using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.UaFx;
using System.Net.Mime;
using System.Text;
using static Org.BouncyCastle.Math.EC.ECCurve;
using System.Threading.Tasks; 

namespace AzureDeviceSdk.Device
{
    public class VirtualDevice
    {
        private readonly DeviceClient client;

        public VirtualDevice(DeviceClient client)
        {
            this.client = client; 
        }


        public async Task sendTelemetryValues(OpcValue[] telemetryValues,string machineId)
        {
          
            var data = new
            {
                production_status = telemetryValues[0].Value,
                workorder_id = telemetryValues[1].Value,
                good_count = telemetryValues[2].Value,
                bad_count = telemetryValues[3].Value,
                temperature = telemetryValues[4].Value
            };

            var dataString = JsonConvert.SerializeObject(data);
            Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
            eventMessage.ContentType = MediaTypeNames.Application.Json;
            eventMessage.ContentEncoding = "utf-8";
            client.SendEventAsync(eventMessage);
            Console.WriteLine($"\t{DateTime.Now.ToLocalTime()} z maszyny {machineId}");
        }


    }
    


}