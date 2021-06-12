﻿using System;
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
        public static string read(NetworkStream stream)
        {
            using (StreamReader sr = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
            {
                return sr.ReadLine();
            }
        }

        public static void write(NetworkStream stream, string msg)
        {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                sw.WriteLine(msg);
            }
        }
    }
}
