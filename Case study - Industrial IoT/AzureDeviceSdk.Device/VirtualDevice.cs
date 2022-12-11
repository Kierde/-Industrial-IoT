using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.UaFx;
using System.Net.Mime;
using System.Text;
using static Org.BouncyCastle.Math.EC.ECCurve;
using System.Threading.Tasks;
using Opc.UaFx.Client;

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

        private async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("Metoda o nazwie:\"{0}\"nie istnieje",methodRequest.Name);
            await Task.Delay(1000);
            return new MethodResponse(0);
        }

        private async Task<MethodResponse> EmergencyStop(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
            var client = new OpcClient("opc.tcp://localhost:4840");
            client.Connect();
            var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder); 
            List<string> listOfIdMachine = new List<string>();
            findMachinesId(node,listOfIdMachine);
            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { numberOfMachine = default(int) });
            object[] result;

            if (payload != null)
            {
                result = client.CallMethod(
                listOfIdMachine[payload.numberOfMachine],
                listOfIdMachine[payload.numberOfMachine] + "/EmergencyStop");

                Console.WriteLine("Maszyna o id:{0} została awaryjnie zatrzymana", listOfIdMachine[payload.numberOfMachine]);
            }
            else 
            {
                for (int i = 0; i < listOfIdMachine.Count - 1; i++)
                {
                    result = client.CallMethod(
                    listOfIdMachine[i + 1],
                    listOfIdMachine[i + 1] + "/EmergencyStop");
                }
                Console.WriteLine("Wszystkie maszyny zostały awaryjnie zatrzymane!");
            }
            await Task.Delay(1000);
            return new MethodResponse(0);
        }

        private async Task<MethodResponse> ResetErrorStatus(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
            var client = new OpcClient("opc.tcp://localhost:4840");
            client.Connect();
            var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
            List<string> listOfIdMachine = new List<string>();
            findMachinesId(node, listOfIdMachine);
            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { numberOfMachine = default(int)});
            object[] result;

            if (payload!= null)
            {
                result = client.CallMethod(
                    listOfIdMachine[payload.numberOfMachine],
                    listOfIdMachine[payload.numberOfMachine] + "/ResetErrorStatus");

                Console.WriteLine("Flagi błędów zostały zresetowane dla maszyny o id:{0}", listOfIdMachine[payload.numberOfMachine]); 
            }
            else
            {
                for (int i = 0; i < listOfIdMachine.Count - 1; i++)
                {
                    result = client.CallMethod(
                    listOfIdMachine[i + 1],
                    listOfIdMachine[i + 1] + "/ResetErrorStatus");
                }
                Console.WriteLine("Flagi błędów zostały zresetowane dla wszystkich maszyn fabryki!");
            }
            await Task.Delay(1000);
            return new MethodResponse(0);
        }

        //odczytanie wszystkich aktywnych maszyn 
        public static void findMachinesId(OpcNodeInfo node, List<string> listOfIdMachine, int level = 0)
        {
            if (level == 1)
            {
                listOfIdMachine.Add(node.NodeId.ToString());
            }
            level++;
            foreach (var childNode in node.Children())
            {
                findMachinesId(childNode, listOfIdMachine, level);
            }
        }
        public async Task InitializeHandlers()
        {
            await client.SetMethodDefaultHandlerAsync(DefaultServiceHandler, client);
            await client.SetMethodHandlerAsync("EmergencyStop", EmergencyStop, client);
            await client.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatus, client); 
        }

    }
}