using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryServer2
{
    class Menu
    {
        public void BeginDataTransmission(NetworkStream stream)
        {
            DatabaseConnector dc = new DatabaseConnector();
            bool logged = false;
            StreamWriter writer = new StreamWriter(stream);
            writer.AutoFlush = true;
            StreamReader reader = new StreamReader(stream);

            do
            {
                String data = reader.ReadLine();
                Console.WriteLine(data);
                string[] logData = checkMessage(data);
                if (logData[0] == "log")
                {
                    if (dc.checkUserData(logData[1], logData[2]))
                    {

                        Console.WriteLine("1");
                        logged = true;
                    }
                    else
                    {
                        Console.WriteLine("0");
                    }
                }
                else if (logData[0] == "reg")
                {
                    if (dc.registerUser(logData[1], logData[2]))
                    {
                        writer.WriteLine("true");
                    }
                    else writer.WriteLine("false");
                }
                else Console.WriteLine("wrong command");
            } while (!logged);

            
            while (logged)
            {
                String choice = reader.ReadLine();
                if (choice == "logout")
                {
                    writer.WriteLine("Zegnam");
                    Thread.Sleep(2000);
                    logged = false;
                }
                else
                {
                }       
            }
        }
        private string[] checkMessage(string s)
        {
            return s.Split(' ');
        }

    }
}
