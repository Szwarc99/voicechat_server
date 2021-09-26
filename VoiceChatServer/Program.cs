using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VoiceChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ServerTCP server = new ServerTCP(8080);
                server.LoopClients();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
