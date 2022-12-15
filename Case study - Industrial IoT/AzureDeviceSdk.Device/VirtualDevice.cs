using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.UaFx;
using System.Net.Mime;
using System.Text;
using Opc.UaFx.Client;
using DeserializationClasses;
using Microsoft.Azure.Devices.Shared;

namespace AzureDeviceSdk.Device
{
    public class VirtualDevice
    {
        private readonly DeviceClient client;

        public VirtualDevice(DeviceClient client)
        {
            this.client = client; 
        }

        public async Task sendTelemetryValues(List<string> messages)
        {
            foreach (var message in messages)
            {
                Message eventMessage = new Message(Encoding.UTF8.GetBytes(message));
                eventMessage.ContentType = MediaTypeNames.Application.Json;
                eventMessage.ContentEncoding = "utf-8";
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message: {message}");
                await client.SendEventAsync(eventMessage);
                await Task.Delay(500);
            }
        }

        //Direct methods 
        private async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("Metoda o nazwie:\"{0}\"nie istnieje",methodRequest.Name);
            await Task.Delay(1000);
            return new MethodResponse(0);
        }

        private async Task<MethodResponse> EmergencyStop(MethodRequest methodRequest, object userContext)
        {
            var twin = await client.GetTwinAsync();
            Console.WriteLine($"\tZostała wywołana metoda o nazwie:{methodRequest.Name}");
            var opcClient = new OpcClient("opc.tcp://localhost:4840");
            opcClient.Connect();
            var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder); 
            List<TeleValueMachine> teleMachineVales = new List<TeleValueMachine>();
            findMachinesId(node, teleMachineVales);
            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { numberOfMachine = default(int) });
            object[] result;

            if (payload != null)
            {
                result = opcClient.CallMethod(
                teleMachineVales[payload.numberOfMachine-1].id_Of_Machine,
                teleMachineVales[payload.numberOfMachine-1].id_Of_Machine + "/EmergencyStop");
                Console.WriteLine("Maszyna o id:{0} została awaryjnie zatrzymana", teleMachineVales[payload.numberOfMachine-1].id_Of_Machine);
            }
            else
            {
                for (int i = 0; i < teleMachineVales.Count; i++)
                {
                    result = opcClient.CallMethod(
                    teleMachineVales[i].id_Of_Machine,
                    teleMachineVales[i].id_Of_Machine + "/EmergencyStop");
                }
                Console.WriteLine("Wszystkie maszyny fabryki zostały awaryjnie zatrzymane!");
            }
            await Task.Delay(1000);
            return new MethodResponse(0);
        }

        private async Task<MethodResponse> MaintenanceDone(MethodRequest methodRequest, object userContext)
        {
            var twin = await client.GetTwinAsync();
            string jsonStr = JsonConvert.SerializeObject(twin);
            DewiceTwin myDeserializedClass = JsonConvert.DeserializeObject<DewiceTwin>(jsonStr);
            Console.WriteLine($"\tZostała wywołana metoda o nazwie:: {methodRequest.Name}");
            var opcClient = new OpcClient("opc.tcp://localhost:4840");
            opcClient.Connect();
            var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);
            List<TeleValueMachine> teleMachineVales = new List<TeleValueMachine>();
            findMachinesId(node, teleMachineVales);
            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { numberOfMachine = default(int) });
            var reportedProperties = new TwinCollection();
            List<string> dateOfLastMaintenance = myDeserializedClass.properties.reported.DateOfLastMaintenance;

            if (payload != null)
            {
                dateOfLastMaintenance[payload.numberOfMachine - 1] = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
                reportedProperties["DateOfLastMaintenance"] = dateOfLastMaintenance;
                await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
                Console.WriteLine("Wykonana została konserwacja maszyny o id:{0}", teleMachineVales[payload.numberOfMachine-1].id_Of_Machine);
            }
            else
            {
                for (int i = 0; i < myDeserializedClass.properties.reported.DateOfLastMaintenance.Count; i++)
                {
                    dateOfLastMaintenance[i] = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
                }
                reportedProperties["DateOfLastMaintenance"] = dateOfLastMaintenance;
                await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
                Console.WriteLine("Wykonana została konserwacja dla wszystkich maszyn fabryki");
            }
            await Task.Delay(1000);
            return new MethodResponse(0);
        }


        private async Task<MethodResponse> ResetErrorStatus(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tZostała wywołana metoda o nazwie: {methodRequest.Name}");
            var opcClient = new OpcClient("opc.tcp://localhost:4840");
            opcClient.Connect();
            var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);
            List<TeleValueMachine> teleMachineVales = new List<TeleValueMachine>();
            findMachinesId(node, teleMachineVales);
            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { numberOfMachine = default(int)});
            object[] result;
           
            if (payload!= null)
            {
                result = opcClient.CallMethod(
                    teleMachineVales[payload.numberOfMachine-1].id_Of_Machine,
                    teleMachineVales[payload.numberOfMachine-1].id_Of_Machine + "/ResetErrorStatus");
                Console.WriteLine("Flagi błędów zostały zresetowane dla maszyny o id:{0}", teleMachineVales[payload.numberOfMachine-1]); 
            }
            else
            {
                for (int i = 0; i < teleMachineVales.Count; i++)
                {
                    result = opcClient.CallMethod(
                    teleMachineVales[i].id_Of_Machine,
                    teleMachineVales[i].id_Of_Machine + "/ResetErrorStatus");
                }
                Console.WriteLine("Flagi błędów zostały zresetowane dla wszystkich maszyn fabryki!");
            }
            await Task.Delay(1000);
            return new MethodResponse(0);
        }
       
        //Device Twin 
        public async Task presetDeviceTwinForUsage()
        {
            Devi twin = await client.GetTwinAsync();
            var opcClient = new OpcClient("opc.tcp://localhost:4840");
            opcClient.Connect();
            var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);
            List<TeleValueMachine> listTeleValuesMachines = new List<TeleValueMachine>();
            string jsonStr = JsonConvert.SerializeObject(twin);


            Console.WriteLine(jsonStr);
            DewiceTwin myDeserializedClass = JsonConvert.DeserializeObject<DewiceTwin>(jsonStr);
            
            var reportedProperties = new TwinCollection();
         

            string[] errorStatus = new string[listTeleValuesMachines.Count - 1];
            string[] dateOfLastMaintenance = new string[listTeleValuesMachines.Count - 1];
            int[] productionRate = new int[listTeleValuesMachines.Count - 1];

            for (int i = 0; i < listTeleValuesMachines.Count; i++)
            {
                errorStatus[i] = "0000";
                dateOfLastMaintenance[i] = "brak informacji o ostatnim o przeglądzie maszyny";
                productionRate[i] = 100;
                OpcStatus productionRateOpc = opcClient.WriteNode(listTeleValuesMachines[i + 1] + "/ProductionRate", 100);
            }

            opcClient.Disconnect(); 
            reportedProperties["DevicesErrors"] = errorStatus;
            reportedProperties["DateOfLastMaintenance"] = dateOfLastMaintenance;
            reportedProperties["ProductionRate"] = productionRate;
            await client.UpdateReportedPropertiesAsync(reportedProperties);
            Console.WriteLine("Desired i reported twin zostały przygotowane");
        }

        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            var opcClient = new OpcClient("opc.tcp://localhost:4840");
            opcClient.Connect();
            var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);
            List<TeleValueMachine> teleMachineVales = new List<TeleValueMachine>();
            findMachinesId(node, teleMachineVales);

            Console.WriteLine($"\tDesired property change:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
            TwinCollection reportedProperties = new TwinCollection();

            var twin = await client.GetTwinAsync();
            string jsonStr = JsonConvert.SerializeObject(twin);
            DewiceTwin myDeserializedClass = JsonConvert.DeserializeObject<DewiceTwin>(jsonStr);


            int[] reportedProductionRate = new int[myDeserializedClass.properties.desired.ProductionRate.Count];
            int i = 0;

            foreach (var value in myDeserializedClass.properties.desired.ProductionRate)
            {
                reportedProductionRate[i] = value;
                OpcStatus productionRateOpc = opcClient.WriteNode(teleMachineVales[i] + "/ProductionRate", value);
                i ++;
            }

            opcClient.Disconnect(); 
            reportedProperties["ProductionRate"] = reportedProductionRate;
            Console.WriteLine("Przed updateReportedProperties");
            await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
            Console.WriteLine("Reported zienione!");
        }


        public async Task InitializeHandlers()
        {
            await client.SetMethodDefaultHandlerAsync(DefaultServiceHandler, client);
            await client.SetMethodHandlerAsync("EmergencyStop", EmergencyStop, client);
            await client.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatus, client);
            await client.SetMethodHandlerAsync("MaintenanceDone", MaintenanceDone, client);
            await client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, client);
        }


        //odczytanie id wszystkich aktywnych maszyn 
        public static void findMachinesId(OpcNodeInfo node, List<TeleValueMachine> teleValuesMachines, int level = 0)
        {
            if (level == 1 && node.NodeId.ToString().Contains("Device"))
            {
                teleValuesMachines.Add(new TeleValueMachine
                {
                    id_Of_Machine = node.NodeId.ToString()
                });
            }
            level++;
            foreach (var childNode in node.Children())
            {
                findMachinesId(childNode, teleValuesMachines, level);
            }
        }
    }
}