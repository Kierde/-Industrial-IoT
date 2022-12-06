using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using Microsoft.Azure.Devices.Client;
using AzureDeviceSdk.Device;
using Microsoft.Azure.Devices.Client.Exceptions;
using Case_study___Industrial_IoT.Properties;
using System.ComponentModel;
using Microsoft.Extensions.Logging.Abstractions;

internal class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            List<string> listOfIdMachine = new List<string>();
            //łaczenie z serwerem maszyn  
            using (var client = new OpcClient("opc.tcp://localhost:4840"))
            {
                client.Connect();
                var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
                findMachinesId(node, listOfIdMachine);
                client.Disconnect(); 
                using var deviceClient = DeviceClient.CreateFromConnectionString(Resources.connectionString, TransportType.Mqtt);
                await deviceClient.OpenAsync();
                var device = new VirtualDevice(deviceClient);

                //przesyłanie wartości telemetrycznych z wszystkich maszyn równolegle do IoTHuba
                //Task[] tasks = new Task[listOfIdMachine.Count-1];
                Task[] connectionTask = new Task[listOfIdMachine.Count- 1];
                VirtualDevice[] devices = new VirtualDevice[listOfIdMachine.Count - 1];
                DeviceClient[] deviceClients = new DeviceClient[listOfIdMachine.Count-1];
                OpcClient[] opcClients = new OpcClient[listOfIdMachine.Count - 1];

                for (int j = 0; j < listOfIdMachine.Count - 1; j++)
                {
                    opcClients[j] = new OpcClient("opc.tcp://localhost:4840");
                    opcClients[j].Connect();
                    deviceClients[j] = DeviceClient.CreateFromConnectionString(Resources.connectionString, TransportType.Mqtt);
                    devices[j] = new VirtualDevice(deviceClients[j]);
                    connectionTask[j] = deviceClients[j].OpenAsync();
                }

                await Task.WhenAll(connectionTask);
                Console.WriteLine("DONE!");


                while (true)
                {
                    Task[] tasks = new Task[listOfIdMachine.Count - 1];
                    for (int i = 0; i < listOfIdMachine.Count - 1; i++)
                    {
                        tasks[i] = taskMachineMethod(listOfIdMachine[i + 1], opcClients[i], devices[i]);
                        Thread.Sleep(1000);
                    }
                    await Task.WhenAll(tasks);
                }

            }

        }
        catch (OpcException e)
        {
            Console.WriteLine("Wiadomość błędu: " + e.Message);
        }

  
        static void findMachinesId(OpcNodeInfo node, List<string> listOfIdMachine, int level = 0)
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

        static OpcReadNode[] readData(string idOfMachine)
        {
            OpcReadNode[] commands = new OpcReadNode[]
            {
                new OpcReadNode(idOfMachine+"/ProductionStatus"),
                new OpcReadNode(idOfMachine+"/WorkorderId"),
                new OpcReadNode(idOfMachine+"/ProductionRate"),
                new OpcReadNode(idOfMachine+"/GoodCount"),
                new OpcReadNode(idOfMachine+"/BadCount"),
                new OpcReadNode(idOfMachine+"/Temperature"),
                new OpcReadNode(idOfMachine+"/DeviceError"),
            };
            return commands;
        }

        async Task taskMachineMethod(string idMachine, OpcClient client, VirtualDevice device)
        {
                Console.WriteLine("Connection success with {0}", idMachine);
                OpcValue[] telemetryValues = new OpcValue[5];
                //odczytywanie wartosci telemetrycznych - production status, workorderId, good, bad, temp 
                  
                telemetryValues[0] = client.ReadNode(idMachine + "/ProductionStatus");
                telemetryValues[1] = client.ReadNode(idMachine + "/WorkorderId");
                telemetryValues[2] = client.ReadNode(idMachine + "/GoodCount");
                telemetryValues[3] = client.ReadNode(idMachine + "/BadCount");
                telemetryValues[4] = client.ReadNode(idMachine + "/Temperature");

                Console.WriteLine("Przed {0}",idMachine);
               //test - sending values 
             await device.sendTelemetryValues(telemetryValues, idMachine);
            Console.WriteLine("Po {0}",idMachine);
        }
    }
}