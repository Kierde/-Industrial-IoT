using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;

internal class Program
{
    private static void Main(string[] args)
    {

        try {

            using (var client = new OpcClient("opc.tcp://localhost:4840"))
            {
                client.Connect();
                var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);

                List<string> listOfIdMachine = new List<string>();
                findMachinesId(node, listOfIdMachine);

                /*    foreach (string str in listOfIdMachine)
                    {
                        Console.WriteLine(str);
                    }
        */

                for (int i = 1; i <= listOfIdMachine.Count - 1; i++)
                {
                    OpcReadNode[] data = readData(listOfIdMachine[i]);

                    IEnumerable<OpcValue> job = client.ReadNodes(data);

                    foreach (var Item in job)
                    {
                        Console.WriteLine(Item.Value);
                    }

                    Console.WriteLine("\n");
                }
            }

        }
        catch (OpcException e)
        {
            Console.WriteLine("Wiadomosc bledu: "+e.Message);
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