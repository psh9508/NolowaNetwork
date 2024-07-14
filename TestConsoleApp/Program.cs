using NolowaNetwork;
using NolowaNetwork.Models.Configuration;
using NolowaNetwork.System;

namespace TestConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            //var rabbitNetwork = new RabbitNetwork(

            var rabbitNetwork = new RabbitNetwork(new MessageCodec());
            rabbitNetwork.Init(new NetworkConfigurationModel()
            {
                HostName = "localhost",
                ExchangeName = "exchangeName",
                ServerName = "serverName",
            });

            Console.ReadKey();
        }
    }
}
