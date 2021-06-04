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

                if (dc.checkUserData(logData[0], logData[1]))
                {

                    Console.WriteLine("1");
                    logged = true;
                }
                else
                {
                    Console.WriteLine("0");
                }
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
