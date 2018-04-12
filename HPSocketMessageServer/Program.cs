using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HPSocketMessageDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            string IPAddress = "127.0.0.1";
            ushort Port = 8355;

            Networking.NetworkServer server = Networking.NetworkServer.Instance();
            server.Start(IPAddress, Port);

            Networking.NetworkActionProcessor processor = new Networking.NetworkActionProcessor(ref Networking.NetworkServer.recvQueue);

            while (true)
            {
                string input = Console.ReadLine();
                Console.WriteLine(input);
            }
        }
    }
}
