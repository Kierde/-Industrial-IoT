using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using Microsoft.Azure.Devices.Client;
using AzureDeviceSdk.Device;
using Microsoft.Azure.Devices.Client.Exceptions;
using Case_study___Industrial_IoT.Properties;

internal class Program
{
    private static async Task Main(string[] args)
    {

        string deviceConnectionString = Resources.connectionString;
        try {
            //połaczenie z IoT Hub 
            using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
            await deviceClient.OpenAsync();
            var device = new VirtualDevice(deviceClient);
            Console.WriteLine("Connection success!");

            try
            {
                //łaczenie z serwerem maszyn  
                using (var client = new OpcClient("opc.tcp://localhost:4840"))
                {
                    client.Connect();
                    var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
                    List<string> listOfIdMachine = new List<string>();
                    findMachinesId(node, listOfIdMachine);

                    OpcReadNode[] data = readData(listOfIdMachine[1]);
                    //show read values 
                    IEnumerable<OpcValue> job = client.ReadNodes(data);

                    foreach (var Item in job)
                    {
                        Console.WriteLine(Item.Value);
                    }

                    OpcValue[] telemetryValues = new OpcValue[5]; 
                    //odczytywanie wartosci telemetrycznych - production status, workorderId, good, bad, temp 
                    telemetryValues[0] = client.ReadNode(listOfIdMachine[1] + "/ProductionStatus");
                    telemetryValues[1] = client.ReadNode(listOfIdMachine[1] + "/WorkorderId");
                    telemetryValues[2] = client.ReadNode(listOfIdMachine[1] + "/GoodCount");
                    telemetryValues[3] = client.ReadNode(listOfIdMachine[1] + "/BadCount");
                    telemetryValues[4] = client.ReadNode(listOfIdMachine[1] + "/Temperature");

                    //test - sending values 
                    await device.sendTelemetryValues(telemetryValues, listOfIdMachine[1]); 





                    /*    foreach (string str in listOfIdMachine)
                        {
                            Console.WriteLine(str);
                        }
            */

                    /*  for (int i = 1; i <= listOfIdMachine.Count - 1; i++)
                      {
                          OpcReadNode[] data = readData(listOfIdMachine[i]);

                          IEnumerable<OpcValue> job = client.ReadNodes(data);

                          foreach (var Item in job)
                          {
                              Console.WriteLine(Item.Value);
                          }

                          Console.WriteLine("\n");
                      }*/
                }

            }
            catch (OpcException e)
            {
                Console.WriteLine("Wiadomość błędu: " + e.Message);
            }

        }
        catch (Exception e)
        {
            Console.WriteLine("Connection failed!" + e.Message);
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


    
    }
}