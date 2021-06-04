using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MemoryServer2
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ServerTCP server = new ServerTCP(IPAddress.Parse("127.0.0.1"), 8080);
                server.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
