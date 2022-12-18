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

        public async Task sendEventMessage(List<string> messages)
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

            ConfigJsonFile cofigFile = readConfigFile(); 
            var twin = await client.GetTwinAsync();
            Console.WriteLine($"\tZostała wywołana metoda o nazwie:{methodRequest.Name}");
            var opcClient = new OpcClient(cofigFile.opc_server_adress);
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
            ConfigJsonFile cofigFile = readConfigFile();
            var twin = await client.GetTwinAsync();
            string jsonStr = JsonConvert.SerializeObject(twin);
            DeviceTwin myDeserializedClass = JsonConvert.DeserializeObject<DeviceTwin>(jsonStr);
            Console.WriteLine($"\tZostała wywołana metoda o nazwie:: {methodRequest.Name}");
            var opcClient = new OpcClient(cofigFile.opc_server_adress);
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
            ConfigJsonFile cofigFile = readConfigFile();
            Console.WriteLine($"\tZostała wywołana metoda o nazwie: {methodRequest.Name}");
            var opcClient = new OpcClient(cofigFile.opc_server_adress);
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
        public async Task presetDeviceTwinForUsage(OpcClient opcClient, List<TeleValueMachine> listTeleValuesMachines)
        {
            opcClient.Connect(); 
            var twin = await client.GetTwinAsync();
            string jsonStr = JsonConvert.SerializeObject(twin);
            DeviceTwin myDeserializedClass = JsonConvert.DeserializeObject<DeviceTwin>(jsonStr);
            var reportedProperties = new TwinCollection();
            string[] errorStatus = new string[listTeleValuesMachines.Count];
            string[] dateOfLastMaintenance = new string[listTeleValuesMachines.Count];
            int[] productionRate = new int[listTeleValuesMachines.Count];
            string[] lastErrorDate = new string[listTeleValuesMachines.Count];

            for (int i = 0; i < listTeleValuesMachines.Count; i++)
            {
                errorStatus[i] = "0000";
                dateOfLastMaintenance[i] = "brak informacji o ostatnim o przeglądzie maszyny";
                lastErrorDate[i] = "brak daty ostatniego błędu";
                productionRate[i] = 100;
                OpcStatus productionRateOpc = opcClient.WriteNode(listTeleValuesMachines[i].id_Of_Machine + "/ProductionRate", 100);
            }
            reportedProperties["DevicesErrors"] = errorStatus;
            reportedProperties["DateOfLastMaintenance"] = dateOfLastMaintenance;
            reportedProperties["ProductionRate"] = productionRate;
            reportedProperties["LastErrorDate"] = lastErrorDate;
            await client.UpdateReportedPropertiesAsync(reportedProperties);
            Console.WriteLine("Desired i reported twin zostały przygotowane");
        }

        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            ConfigJsonFile cofigFile = readConfigFile();
            var twin = await client.GetTwinAsync();
            string jsonStr = JsonConvert.SerializeObject(twin);
            DeviceTwin myDeserializedClass = JsonConvert.DeserializeObject<DeviceTwin>(jsonStr);
            var opcClient = new OpcClient(cofigFile.opc_server_adress);
            opcClient.Connect();
            var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);
            List<TeleValueMachine> teleMachineVales = new List<TeleValueMachine>();
            findMachinesId(node, teleMachineVales);
            Console.WriteLine($"\tProperty desired zostały zmienione:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
            TwinCollection reportedProperties = new TwinCollection();

            List<int> desiredProductionRates = myDeserializedClass.properties.desired.ProductionRate;
            int i = 0;

            foreach (var productionRate in desiredProductionRates)
            {
                OpcStatus productionRateOpc = opcClient.WriteNode(teleMachineVales[i].id_Of_Machine + "/ProductionRate", productionRate);
                i ++;
            }
        }
        
        public async Task InitializeHandlers()
        {
            await client.SetMethodDefaultHandlerAsync(DefaultServiceHandler, client);
            await client.SetMethodHandlerAsync("EmergencyStop", EmergencyStop, client);
            await client.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatus, client);
            await client.SetMethodHandlerAsync("MaintenanceDone", MaintenanceDone, client);
            await client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, client);
        }


        public async Task updateReportedProductionRate(List<TeleValueMachine> toReport)
        {
            var twin = await client.GetTwinAsync();
            string jsonStr = JsonConvert.SerializeObject(twin); 
            DeviceTwin deviceTwin = JsonConvert.DeserializeObject<DeviceTwin>(jsonStr);
            var reportedProperties =new TwinCollection();
            List<int> newProductionRate = new List<int>();
            newProductionRate = deviceTwin.properties.reported.ProductionRate;

            foreach (var machine in toReport)
            {
                int numberOfMachine = (int)Char.GetNumericValue(machine.id_Of_Machine[machine.id_Of_Machine.Length-1]);
                newProductionRate[numberOfMachine-1] = machine.production_rate;
            }

            reportedProperties["ProductionRate"] = newProductionRate;
            await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
            Console.WriteLine("reported twin updated - production rate");
        }

        public async Task updateReportedErrorsSendEvent(List<TeleValueMachine> toReport,List<string> evenMessageError)
        {
            var twin = await client.GetTwinAsync();
            string jsonStr = JsonConvert.SerializeObject(twin);
            DeviceTwin deviceTwin = JsonConvert.DeserializeObject<DeviceTwin>(jsonStr);
            var reportedProperties = new TwinCollection();
            List<string> newErrorFlags = new List<string>();
            List<string> newLastErrorDate = new List<string>(); 
            newErrorFlags = deviceTwin.properties.reported.DevicesErrors;
            newLastErrorDate = deviceTwin.properties.reported.LastErrorDate;

            foreach(var machine in toReport)
            {
                int numberOfMachine = (int)Char.GetNumericValue(machine.id_Of_Machine[machine.id_Of_Machine.Length - 1]);

                if(machine.device_error==0)
                    newErrorFlags[numberOfMachine - 1] = "0000";
                else if(machine.device_error == 1)
                    newErrorFlags[numberOfMachine - 1] = "0001";
                else if (machine.device_error == 2)
                    newErrorFlags[numberOfMachine - 1] = "0010";
                else if (machine.device_error == 4)
                    newErrorFlags[numberOfMachine - 1] = "0100";
                else if (machine.device_error == 8)
                    newErrorFlags[numberOfMachine - 1] = "1000";

                newLastErrorDate[numberOfMachine-1] = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
            }

            reportedProperties["DevicesErrors"] = newErrorFlags;
            reportedProperties["LastErrorDate"] = newLastErrorDate;
            await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
            Console.WriteLine("reported twin updated - error flags and date of last error");
            await sendEventMessage(evenMessageError);
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


        public static ConfigJsonFile readConfigFile()
        {
            StreamReader r = new StreamReader("..\\..\\..\\..\\configurationFile.json");
            string configFileContent = r.ReadToEnd();
            ConfigJsonFile configJsonFIile = JsonConvert.DeserializeObject<ConfigJsonFile>(configFileContent);
            return configJsonFIile; 
        }
    }
}