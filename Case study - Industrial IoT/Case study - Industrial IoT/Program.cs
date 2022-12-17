using AzureDeviceSdk.Device;
using Case_study___Industrial_IoT.Properties;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx.Client;
using Opc.UaFx;
using Newtonsoft.Json;
using DeserializationClasses;


internal class Program
{
    private static async Task Main(string[] args)
    {
        List<TeleValueMachine> teleValuesMachines = new List<TeleValueMachine>();
        List<TeleValueMachine> oldTeleValues = new List<TeleValueMachine>();
        List<TeleValueMachine> toReport = new List<TeleValueMachine>();

        try
        {
            using (var client = new OpcClient("opc.tcp://localhost:4840"))
            {
                client.Connect();
                var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
                VirtualDevice.findMachinesId(node, teleValuesMachines);
                VirtualDevice.findMachinesId(node, oldTeleValues);
                using var deviceClient = DeviceClient.CreateFromConnectionString(Resources.connectionString, TransportType.Mqtt);
                await deviceClient.OpenAsync();
                var device = new VirtualDevice(deviceClient);
                Console.WriteLine("Połączenia udane");
                await device.InitializeHandlers();
                Console.WriteLine("Inicjalizacja udana");
                await device.presetDeviceTwinForUsage(client, teleValuesMachines);
                readTeleValues(oldTeleValues, client);



                while (true)
                {
                    readTeleValues(teleValuesMachines, client);
                   
                    await device.sendEventMessage(prepTelemetryMessage(teleValuesMachines));
                    await isValueChanged(oldTeleValues, teleValuesMachines, device);
            
                    await Task.Delay(4000);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }


        static void readTeleValues(List<TeleValueMachine> teleValueMachines, OpcClient client)
        {
           
            foreach (TeleValueMachine teleMachine in teleValueMachines)
            {
                teleMachine.production_status = (int)client.ReadNode(teleMachine.id_Of_Machine + "/ProductionStatus").Value;
                teleMachine.workorder_id = (string)client.ReadNode(teleMachine.id_Of_Machine + "/WorkorderId").Value;
                teleMachine.good_count = (int)(long)client.ReadNode(teleMachine.id_Of_Machine + "/GoodCount").Value;
                teleMachine.bad_count = (int)(long)client.ReadNode(teleMachine.id_Of_Machine + "/BadCount").Value;
                teleMachine.temperature = (double)client.ReadNode(teleMachine.id_Of_Machine + "/Temperature").Value;
                teleMachine.production_rate = (int)client.ReadNode(teleMachine.id_Of_Machine + "/ProductionRate").Value;
                teleMachine.device_error = (int)client.ReadNode(teleMachine.id_Of_Machine + "/DeviceError").Value;
            }

        }

       async Task isValueChanged(List<TeleValueMachine> old, List<TeleValueMachine> now, VirtualDevice device)
        {
            List<TeleValueMachine> toReport = new List<TeleValueMachine>();
            List<ErrorMessage> errorInfoToSend = new List<ErrorMessage>(); 
            int flag = 0; 

            for (int i = 0; i < old.Count; i++)
            {
                if (old[i].production_rate != now[i].production_rate)
                {
                    flag = 1;
                    toReport.Add(now[i]);
                    old[i].production_rate = now[i].production_rate;
                }
                else if (old[i].device_error != now[i].device_error)
                {
                    flag = 2;
                    toReport.Add(now[i]);
                    old[i].device_error = now[i].device_error;

                    var errorMessage = new ErrorMessage(now[i].id_Of_Machine, now[i].device_error);
                    errorInfoToSend.Add(errorMessage);
                  
                }
            }
            if(toReport.Count > 0 && flag==1)
                await device.updateReportedProductionRate(toReport);

           if (toReport.Count > 0 && flag == 2)
               await device.updateReportedErrorsSendEvent(toReport,prepErrorMessage(errorInfoToSend));
        }



        static List<string> prepErrorMessage(List<ErrorMessage> errorMessages)
        {
            List<string> prepString = new List<string>();

            foreach (ErrorMessage message in errorMessages)
            {
                var jsonString = JsonConvert.SerializeObject(message);
                prepString.Add(jsonString);
            }
            return prepString;
        }



        static List<string> prepTelemetryMessage(List<TeleValueMachine> teleValuesMachines)
        {
            List<string> prepString = new List<string>();

            foreach (TeleValueMachine machineTele in teleValuesMachines)
            {
                var jsonString = JsonConvert.SerializeObject(machineTele);
                prepString.Add(jsonString);
            }
            return prepString;
        }
    }
}