using AzureDeviceSdk.Device;
using Case_study___Industrial_IoT.Properties;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx.Client;
using Opc.UaFx;
using Newtonsoft.Json;
using DeserializationClasses;


List<TeleValueMachine> teleValuesMachines = new List<TeleValueMachine>();

try
{
    using (var client = new OpcClient("opc.tcp://localhost:4840"))
    {
        client.Connect();
        var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
        VirtualDevice.findMachinesId(node, teleValuesMachines);
        using var deviceClient = DeviceClient.CreateFromConnectionString(Resources.connectionString,TransportType.Mqtt);
        await deviceClient.OpenAsync();
        var device = new VirtualDevice(deviceClient);
        Console.WriteLine("Połączenia udane");
        await device.InitializeHandlers();
        Console.WriteLine("Inicjalizacja udana");


        while (true)
        {
            readTeleValues(teleValuesMachines, client);
            await device.sendTelemetryValues(prepData(teleValuesMachines));
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
    client.Connect();
    foreach(TeleValueMachine teleMachine in teleValueMachines)
    {
        teleMachine.production_status = (int) client.ReadNode(teleMachine.id_Of_Machine + "/ProductionStatus").Value;
        teleMachine.workorder_id = (string)client.ReadNode(teleMachine.id_Of_Machine + "/WorkorderId").Value;
        teleMachine.good_count = (int)(long)client.ReadNode(teleMachine.id_Of_Machine + "/GoodCount").Value;
        teleMachine.bad_count = (int)(long)client.ReadNode(teleMachine.id_Of_Machine + "/BadCount").Value;
        teleMachine.temperature = (double)client.ReadNode(teleMachine.id_Of_Machine + "/Temperature").Value;
    }
}

static List<string> prepData(List<TeleValueMachine> teleValuesMachines)
{
    List<string> prepString = new List<string>();

    foreach(TeleValueMachine machineTele in teleValuesMachines)
    {
        var jsonString = JsonConvert.SerializeObject(machineTele);
        prepString.Add(jsonString); 
    }

    return prepString;
}
