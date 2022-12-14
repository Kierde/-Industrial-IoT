using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using Microsoft.Azure.Devices.Client;
using AzureDeviceSdk.Device;
using Microsoft.Azure.Devices.Client.Exceptions;
using Case_study___Industrial_IoT.Properties;
using System.ComponentModel;
using Microsoft.Extensions.Logging.Abstractions;
using Org.BouncyCastle.Ocsp;
using System.Threading.Tasks;
using System.Reflection.PortableExecutable;


internal class Program
{
    private static async Task Main(string[] args)
    {

       

        List<string> listOfIdMachine = new List<string>();

        //otrzymywanie listy aktywnych maszyn 
        var client = new OpcClient("opc.tcp://localhost:4840");
        
            client.Connect();
            var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
            VirtualDevice.findMachinesId(node, listOfIdMachine);
            client.Disconnect();
        
       
        //przesyłanie wartości telemetrycznych z wszystkich maszyn równolegle do IoTHuba
      
        OpcClient[] opcClients = new OpcClient[listOfIdMachine.Count - 1];
        DeviceClient[] deviceClients = new DeviceClient[listOfIdMachine.Count - 1];
        VirtualDevice[] devices = new VirtualDevice[listOfIdMachine.Count - 1];
        Task[] connectionTask = new Task[listOfIdMachine.Count - 1];
        Task[] initTask = new Task[listOfIdMachine.Count - 1];

        
        //try catch - do zrobienia 
        for (int j = 0; j < listOfIdMachine.Count - 1; j++)  
        {
            opcClients[j] = new OpcClient("opc.tcp://localhost:4840");
            deviceClients[j] = DeviceClient.CreateFromConnectionString(Resources.connectionString, TransportType.Mqtt);
            connectionTask[j] = deviceClients[j].OpenAsync();
            devices[j] = new VirtualDevice(deviceClients[j]);
            initTask[j] = devices[j].InitializeHandlers(); 
        }
        await Task.WhenAll(connectionTask);
        await Task.Delay(1000);
        Console.WriteLine("Połączenia udane");
        await Task.WhenAll(initTask);
        await Task.Delay(1000);
        
        Console.WriteLine("Inicjalizacja się powiodła");
        

        var deviceClientPre = DeviceClient.CreateFromConnectionString(Resources.connectionString, TransportType.Mqtt);
        var connectionTPre = deviceClientPre.OpenAsync();
        var devicesPre = new VirtualDevice(deviceClientPre);
        await devicesPre.presetDeviceTwinForUsage(); 



        
        await Task.Delay(2500);

        Task[] tasks = new Task[listOfIdMachine.Count - 1];

        var parllelOpition = new ParallelOptions { MaxDegreeOfParallelism =listOfIdMachine.Count-1};


        // for (int j = 0; j < 2; j++)*/
        while (true)
        {
            for (int i = 0; i < listOfIdMachine.Count - 1; i++)
            {
            
                //tasks[i] = taskMachineMethod(listOfIdMachine[i + 1], opcClients[i], devices[i]);
            }
           //  await Task.WhenAll(tasks);

            await Task.Delay(5000);
        }

        async Task taskMachineMethod(string idMachine, OpcClient client, VirtualDevice device)
        {  
               // Console.WriteLine("Connection success with {0}", idMachine);
                OpcValue[] telemetryValues = new OpcValue[5];
                client.Connect();
            //odczytywanie wartosci telemetrycznych - production status, workorderId, good, bad, temperatura 
                telemetryValues[0] = client.ReadNode(idMachine + "/ProductionStatus");
                telemetryValues[1] = client.ReadNode(idMachine + "/WorkorderId");
                telemetryValues[2] = client.ReadNode(idMachine + "/GoodCount");
                telemetryValues[3] = client.ReadNode(idMachine + "/BadCount");
                telemetryValues[4] = client.ReadNode(idMachine + "/Temperature");
                client.Disconnect(); 
                await device.sendTelemetryValues(telemetryValues, idMachine);
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()} z maszyny {idMachine}");
        }


    }

    
}