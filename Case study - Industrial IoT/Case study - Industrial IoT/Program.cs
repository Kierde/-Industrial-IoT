using AzureDeviceSdk.Device;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx.Client;
using Opc.UaFx;
using Newtonsoft.Json;
using DeserializationClasses;


internal class Program
{
    private static async Task Main(string[] args)
    {
        ConfigJsonFile cofigFile= VirtualDevice.readConfigFile();
        List<TeleValueMachine> teleValuesMachines = new List<TeleValueMachine>();
        List<TeleValueMachine> oldTeleValues = new List<TeleValueMachine>();
        List<List<int>> goodCount = new List<List<int>>();
        List<List<int>> badCount = new List<List<int>>();

        try
        {
            using (var client = new OpcClient(cofigFile.opc_server_adress))
            {
                client.Connect();
                var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
                VirtualDevice.findMachinesId(node, teleValuesMachines);
                VirtualDevice.findMachinesId(node, oldTeleValues);

                for(int k = 0; k < teleValuesMachines.Count; k++)
                {
                    List<int> good = new List<int>();
                    List<int> bad = new List<int>();
                    goodCount.Add(good);
                    badCount.Add(bad); 
                }
                using var deviceClient = DeviceClient.CreateFromConnectionString(cofigFile.iot_connection_string, TransportType.Mqtt);
                await deviceClient.OpenAsync();
                var device = new VirtualDevice(deviceClient);
                Console.WriteLine("Połączenia udane");
                await device.InitializeHandlers();
                Console.WriteLine("Inicjalizacja udana");
                await device.presetDeviceTwinForUsage(client, teleValuesMachines);
                readTeleValues(oldTeleValues, oldTeleValues, client, badCount, goodCount);
                Console.WriteLine("Obecnie działające linie produkcyjne mają numery id:");

                foreach (TeleValueMachine teleValueMachine in teleValuesMachines) 
                {
                    Console.WriteLine(teleValueMachine.id_Of_Machine); 
                }
                
                while (true)
                {
                    readTeleValues(teleValuesMachines, oldTeleValues,client,badCount, goodCount);
                    await device.sendEventMessage(prepTelemetryMessage(teleValuesMachines));
                    await isValueChanged(teleValuesMachines, oldTeleValues, device,badCount,goodCount);
                    await Task.Delay(4000);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        static void readTeleValues(List<TeleValueMachine> teleValueMachines,List<TeleValueMachine> old,OpcClient client, List<List<int>> badC, List<List<int>> goodC)
        {
            int i = 0;
         
            foreach (TeleValueMachine teleMachine in teleValueMachines)
            {
                teleMachine.workorder_id = (string)client.ReadNode(teleMachine.id_Of_Machine + "/WorkorderId").Value;
                if (old[i].workorder_id != teleMachine.workorder_id && teleMachine.workorder_id != "00000000-0000-0000-0000-000000000000" && old[i].workorder_id!= "00000000-0000-0000-0000-000000000000")
                {
                    goodC[i].Add(teleMachine.good_count);
                    badC[i].Add(teleMachine.bad_count);
                    old[i].workorder_id = teleMachine.workorder_id;
                }
                int sumGood = 0;
                int sumBad = 0;

                if (badC.Count!=0 && badC[i].Count!=0)
                foreach (int bad in badC[i])
                {
                    sumBad += bad;
                }

                if (badC.Count != 0 && badC[i].Count != 0)

                foreach (int good in goodC[i])
                {
                    sumGood += good;
                }
                teleMachine.production_status = (int)client.ReadNode(teleMachine.id_Of_Machine + "/ProductionStatus").Value;
                teleMachine.good_count = (int)(long)client.ReadNode(teleMachine.id_Of_Machine + "/GoodCount").Value - sumGood;
                teleMachine.bad_count = (int)(long)client.ReadNode(teleMachine.id_Of_Machine + "/BadCount").Value - sumBad;
                teleMachine.temperature = (double)client.ReadNode(teleMachine.id_Of_Machine + "/Temperature").Value;
                teleMachine.production_rate = (int)client.ReadNode(teleMachine.id_Of_Machine + "/ProductionRate").Value;
                teleMachine.device_error = (int)client.ReadNode(teleMachine.id_Of_Machine + "/DeviceError").Value;
                i++; 
            }
        }

       async Task isValueChanged(List<TeleValueMachine> now, List<TeleValueMachine> old, VirtualDevice device, List<List<int>> badC, List<List<int>> goodC)
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