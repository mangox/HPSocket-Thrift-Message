using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageProtocol;
namespace HPSocketMessageClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Networking.NetworkProcessor procesor = new Networking.NetworkProcessor();
            procesor.Run();
            //var client = Networking.NetworkClient.Instance();
            //client.Connect("127.0.0.1", 8355);
            
            while (true)
            {
                /*
                MessageProtocol.RequestTask rt = new RequestTask();
                rt.ClientId = "client#1";
                rt.Version = "1.0.1";
                rt.Message = "";
                
                Package rtPkg = new Package() { PackType = PackType.RequestTask, Content = rt };
                client.Send(rtPkg);
                */
                string input = Console.ReadLine();
                Console.WriteLine(input);
            }
        }
    }
}
