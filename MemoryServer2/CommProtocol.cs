using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MemoryServer2
{
    class CommProtocol
    {
        public Dictionary<NetworkStream, string> clientKeys = new Dictionary<NetworkStream,string>();

        public static string Read(NetworkStream stream)
        {
            using (StreamReader sr = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
            {
                return sr.ReadLine();
            }
        }

        public static void Write(NetworkStream stream, string msg)
        {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                try
                {
                    sw.WriteLine(msg);
                }
                catch (Exception e)
                { }
            }
        }
        public static string[] CheckMessage(string sData)
        {
            return sData.Split(' ');
        }
    }
}
