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
            List<Room> rooms = new List<Room>();
            bool logged = false;
            StreamWriter writer = new StreamWriter(stream);
            writer.AutoFlush = true;
            StreamReader reader = new StreamReader(stream);
            rooms.Add(new Room(0, false, "admin"));

            do
            {
                String data = reader.ReadLine();
                Console.WriteLine(data);
                string[] logData = checkMessage(data);
                if (logData[0] == "log")
                {
                    if (dc.checkUserData(logData[1], logData[2]))
                    {

                        Console.WriteLine("user logged");
                        writer.WriteLine("1");
                        logged = true;
                    }
                    else
                    {
                        writer.WriteLine("!");
                        Console.WriteLine("wrong login data");
                    }
                }
                else if (logData[0] == "reg")
                {
                    if (dc.registerUser(logData[1], logData[2]))
                    {
                        writer.WriteLine("1");
                    }
                    else writer.WriteLine("!");
                }
                else Console.WriteLine("wrong command");
            } while (!logged);


            
            while (logged)
            {
                String data = reader.ReadLine();
                string[] logData = checkMessage(data);
                if (data == "logout")
                {
                    Thread.Sleep(2000);
                    logged = false;
                }
                else if (logData[0]=="ref")
                {
                    foreach (Room room in rooms)
                    {
                        string msg = "rom " + room.id + " " + room.isPrivate + " " + room.activeUsers + " " + room.begun;

                        if (room == rooms.Last())
                        {
                            msg = msg + " end";
                        }
                        writer.Write(msg);
                    }
                }    
            }
        }
        private string[] checkMessage(string s)
        {
            return s.Split(' ');
        }

    }
}
