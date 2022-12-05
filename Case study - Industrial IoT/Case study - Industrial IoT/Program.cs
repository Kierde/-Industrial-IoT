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
                client.Disconnect();
            }

            var task1 = threadMachineMethod(listOfIdMachine[1]);
            var task2 = threadMachineMethod(listOfIdMachine[2]);
            await Task.WhenAll(task1, task2); 

           



               

/*
            for(int i=1; i<listOfIdMachine.Count-1;i++)
                await Task.Run(() => threadMachineMethod(listOfIdMachine[i]));*/
            //await threadMachineMethod(listOfIdMachine[1]);



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

        async Task threadMachineMethod(string idMachine)
        {
            try
            {
                using var deviceClient = DeviceClient.CreateFromConnectionString(Resources.connectionString, TransportType.Mqtt);
                await deviceClient.OpenAsync();
                var device = new VirtualDevice(deviceClient);
                Console.WriteLine("Connection success with {0}", idMachine);

                try
                {
                    using (var client = new OpcClient("opc.tcp://localhost:4840"))
                    {
                        client.Connect();
                        while (true)
                        {
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
                catch (Exception e)
                {
                    Console.WriteLine("Wiadomość błędu: " + e.Message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection failed!" + e.Message);
            }
        }
    }
}