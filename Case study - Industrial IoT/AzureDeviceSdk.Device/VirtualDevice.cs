using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.UaFx;
using System.Net.Mime;
using System.Text;
using Opc.UaFx.Client;
using DeserializationClasses;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualBasic;

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
            Console.WriteLine($"Wysyłanie waidomości {DateTime.Now.ToLocalTime()} do maszyny {machineId}");
            await client.SendEventAsync(eventMessage);
           // Console.WriteLine($"\t{DateTime.Now.ToLocalTime()} z maszyny {machineId}");

        }

        private async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("Metoda o nazwie:\"{0}\"nie istnieje",methodRequest.Name);
            await Task.Delay(1000);
            return new MethodResponse(0);
        }

        //Direct methods 
        private async Task<MethodResponse> EmergencyStop(MethodRequest methodRequest, object userContext)
        {
            var twin = await client.GetTwinAsync();
            string jsonStr = JsonConvert.SerializeObject(twin);
            Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(jsonStr);
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
            var opcClient = new OpcClient("opc.tcp://localhost:4840");
            opcClient.Connect();
            var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder); 
            List<string> listOfIdMachine = new List<string>();
            findMachinesId(node,listOfIdMachine);
            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { numberOfMachine = default(int) });
            object[] result;
            var reportedProperties = new TwinCollection();
            string[] deviceErrorsFlag = new string[myDeserializedClass.properties.reported.DevicesErrors.Count];

            int j = 0;
            foreach (string val in myDeserializedClass.properties.reported.DevicesErrors)
            {
                deviceErrorsFlag[j++] = val;
            }

            if (payload != null)
            {
                result = opcClient.CallMethod(
                listOfIdMachine[payload.numberOfMachine],
                listOfIdMachine[payload.numberOfMachine] + "/EmergencyStop");

                deviceErrorsFlag[payload.numberOfMachine - 1] = "0001";
                reportedProperties["DevicesErrors"] = deviceErrorsFlag;
                await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
                Console.WriteLine("Maszyna o id:{0} została awaryjnie zatrzymana", listOfIdMachine[payload.numberOfMachine]);
            }
            else 
            {
                for (int i = 0; i < listOfIdMachine.Count - 1; i++)
                {
                    result = opcClient.CallMethod(
                    listOfIdMachine[i + 1],
                    listOfIdMachine[i + 1] + "/EmergencyStop");
                    deviceErrorsFlag[i] = "0001";
                }


                reportedProperties["DevicesErrors"] = deviceErrorsFlag;
                await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
                Console.WriteLine("Wszystkie maszyny zostały awaryjnie zatrzymane!");
            }
            await Task.Delay(1000);
            return new MethodResponse(0);
        }

        private async Task<MethodResponse> MaintenanceDone(MethodRequest methodRequest, object userContext)
        {
            var twin = await client.GetTwinAsync();
            string jsonStr = JsonConvert.SerializeObject(twin);
            Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(jsonStr);


            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
            var opcClient = new OpcClient("opc.tcp://localhost:4840");
            opcClient.Connect();
            var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);
            List<string> listOfIdMachine = new List<string>();
            findMachinesId(node, listOfIdMachine);
            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { numberOfMachine = default(int) });

            var reportedProperties = new TwinCollection();

            string[] dateOfMaintanace = new string[myDeserializedClass.properties.reported.DateOfLastMaintenance.Count];

            int i = 0;
            foreach (string val in myDeserializedClass.properties.reported.DateOfLastMaintenance)
            {
                dateOfMaintanace[i++] = val ;
            }


            if (payload != null)
            {
                Console.WriteLine("Wykonana została konserwacja maszyny o id:{0}", listOfIdMachine[payload.numberOfMachine]);
                dateOfMaintanace[payload.numberOfMachine - 1] = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
                reportedProperties["DateOfLastMaintenance"] = dateOfMaintanace;
                await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
            }
            else
            {
                for (int j = 0; j < myDeserializedClass.properties.reported.DateOfLastMaintenance.Count; j++)
                {
                    dateOfMaintanace[j] = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
                }
                reportedProperties["DateOfLastMaintenance"] = dateOfMaintanace;
                await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
                Console.WriteLine("Wykonana została konserwacja dla wszystkich maszyn fabryki!");
            }
            await Task.Delay(1000);
           
            return new MethodResponse(0);
        }



      
        private async Task<MethodResponse> ResetErrorStatus(MethodRequest methodRequest, object userContext)
        {
            var twin = await client.GetTwinAsync();
            string jsonStr = JsonConvert.SerializeObject(twin);
            Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(jsonStr);
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
            var opcClient = new OpcClient("opc.tcp://localhost:4840");
            opcClient.Connect();
            var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);
            List<string> listOfIdMachine = new List<string>();
            findMachinesId(node, listOfIdMachine);
            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { numberOfMachine = default(int)});
            object[] result;
            var reportedProperties = new TwinCollection();
            string[] deviceErrorsFlag = new string[myDeserializedClass.properties.reported.DevicesErrors.Count];

            int j = 0; 
            foreach (string val in myDeserializedClass.properties.reported.DevicesErrors)
            {
                deviceErrorsFlag[j++] = val;
            }

            if (payload!= null)
            {
                result = opcClient.CallMethod(
                    listOfIdMachine[payload.numberOfMachine],
                    listOfIdMachine[payload.numberOfMachine] + "/ResetErrorStatus");
                deviceErrorsFlag[payload.numberOfMachine - 1] = "0000";
                reportedProperties["DevicesErrors"] = deviceErrorsFlag;
                await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
                Console.WriteLine("Flagi błędów zostały zresetowane dla maszyny o id:{0}", listOfIdMachine[payload.numberOfMachine]); 
            }
            else
            {
                for (int i = 0; i < listOfIdMachine.Count-1; i++)
                {
                    result = opcClient.CallMethod(
                    listOfIdMachine[i + 1],
                    listOfIdMachine[i + 1] + "/ResetErrorStatus");
                    deviceErrorsFlag[i] = "0000";
                }
                reportedProperties["DevicesErrors"] = deviceErrorsFlag;
                await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
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


        //Device Twin 
        public async Task presetDeviceTwinForUsage()
        {
            var twin = await client.GetTwinAsync();

            var opcClient = new OpcClient("opc.tcp://localhost:4840");
            opcClient.Connect();
            var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);
            List<string> listOfIdMachine = new List<string>();
            findMachinesId(node, listOfIdMachine);

            // Console.WriteLine($"\nInitial twin value received: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");
            // Console.WriteLine();
           string jsonStr = JsonConvert.SerializeObject(twin);
           Console.WriteLine(jsonStr);
           Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(jsonStr);
            
            var reportedProperties = new TwinCollection();
            var desiredProperties = new TwinCollection(); 

            string[] errorStatus = new string[listOfIdMachine.Count - 1];
            string[] dateOfLastMaintenance = new string[listOfIdMachine.Count - 1];
            int[] productionRate = new int[listOfIdMachine.Count - 1];

         

            for (int i = 0; i < listOfIdMachine.Count - 1; i++)
            {
                errorStatus[i] = "0000";
                dateOfLastMaintenance[i] = "brak informacji";
                productionRate[i] = 100;
                OpcStatus productionRateOpc = opcClient.WriteNode(listOfIdMachine[i + 1] + "/ProductionRate", 100);
            }
            opcClient.Disconnect(); 
            reportedProperties["DevicesErrors"] = errorStatus;
            reportedProperties["DateOfLastMaintenance"] = dateOfLastMaintenance;
            reportedProperties["ProductionRate"] = productionRate;
           

            
            await client.UpdateReportedPropertiesAsync(reportedProperties);
            Console.WriteLine("Desired i reported twin zostały przygotowane");
            
            client.Dispose();
        }

        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            var opcClient = new OpcClient("opc.tcp://localhost:4840");
            opcClient.Connect();
            var node = opcClient.BrowseNode(OpcObjectTypes.ObjectsFolder);
            List<string> listOfIdMachine = new List<string>();
            findMachinesId(node, listOfIdMachine);

            Console.WriteLine($"\tDesired property change:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
           
            TwinCollection reportedProperties = new TwinCollection();

            var twin = await client.GetTwinAsync();
            string jsonStr = JsonConvert.SerializeObject(twin);
            Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(jsonStr);


            int[] reportedProductionRate = new int[myDeserializedClass.properties.desired.ProductionRate.Count];
            int i = 0;

            foreach (var value in myDeserializedClass.properties.desired.ProductionRate)
            {
                reportedProductionRate[i] = value;
                OpcStatus productionRateOpc = opcClient.WriteNode(listOfIdMachine[i + 1] + "/ProductionRate", value);
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
    }
}