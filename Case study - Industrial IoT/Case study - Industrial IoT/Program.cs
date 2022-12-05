using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using Microsoft.Azure.Devices.Client;
using AzureDeviceSdk.Device;
using Microsoft.Azure.Devices.Client.Exceptions;
using Case_study___Industrial_IoT.Properties;
using System.ComponentModel;

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

                using var deviceClient = DeviceClient.CreateFromConnectionString(Resources.connectionString, TransportType.Mqtt);
                await deviceClient.OpenAsync();
                var device = new VirtualDevice(deviceClient);

                //Task[] tasks = new Task[2];
                Task[] tasks = new Task[listOfIdMachine.Count-1];

                while (true)
                {
                    for (int i = 0; i < listOfIdMachine.Count - 1; i++) 
                    {
                        tasks[i]= threadMachineMethod(listOfIdMachine[i+1], client, device);
                    }
                    await Task.WhenAll(tasks);
                    // tasks[0] = threadMachineMethod(listOfIdMachine[1],client, device);
                    //tasks[1] = threadMachineMethod(listOfIdMachine[2],client, device);
                    //await Task.WhenAll(tasks);
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

        async Task threadMachineMethod(string idMachine, OpcClient client, VirtualDevice device)
        { 
                Console.WriteLine("Connection success with {0}", idMachine);
                OpcValue[] telemetryValues = new OpcValue[5];
              //odczytywanie wartosci telemetrycznych - production status, workorderId, good, bad, temp 
                telemetryValues[0] = client.ReadNode(idMachine + "/ProductionStatus");
                telemetryValues[1] = client.ReadNode(idMachine + "/WorkorderId");
                telemetryValues[2] = client.ReadNode(idMachine + "/GoodCount");
                telemetryValues[3] = client.ReadNode(idMachine + "/BadCount");
                telemetryValues[4] = client.ReadNode(idMachine + "/Temperature");
            //test - sending values 
            await device.sendTelemetryValues(telemetryValues, idMachine); 
        }
    }
}