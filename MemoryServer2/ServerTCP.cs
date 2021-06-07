using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryServer2
{
    class ServerTCP
    {
        private TcpListener _server;
        private Boolean _isRunning;

        public ServerTCP(int port)
        {
            _server = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            _server.Start();

            _isRunning = true;
            
            LoopClients();
        }

        public void LoopClients()
        {
            List<Room> rooms = new List<Room>();
            while (_isRunning)
            {
                // wait for client connection
               // object[] vs = new object[2];
                TcpClient newClient = _server.AcceptTcpClient();
                //vs[0] = newClient;
                //vs[1] = rooms;
                // client found.
                // create a thread to handle communication

                Thread t = new Thread(unused => HandleClient(rooms,newClient)
  );
                t.Start(newClient);
            }
        }

        public void HandleClient(List<Room> roms, object obj)
        {
            List<Room> rooms =roms;
            // retrieve client from parameter passed to thread
            TcpClient client = (TcpClient)obj;

            // sets two streams
            StreamWriter sWriter = new StreamWriter(client.GetStream(), Encoding.ASCII);
            StreamReader sReader = new StreamReader(client.GetStream(), Encoding.ASCII);
            // you could use the NetworkStream to read and write, 
            // but there is no forcing flush, even when requested

            Boolean bClientConnected = true;
            String sData = null;
            DatabaseConnector dc = new DatabaseConnector();
            bool logged = false;
            sWriter.AutoFlush = true;

            while (bClientConnected)
            {
                do
                {
                    sData = sReader.ReadLine();
                    Console.WriteLine(sData);
                    string[] logData = checkMessage(sData);
                    if (logData[0] == "log")
                    {
                        if (dc.checkUserData(logData[1], logData[2]))
                        {

                            Console.WriteLine("user logged");
                            sWriter.WriteLine("1");
                            logged = true;
                        }
                        else
                        {
                            sWriter.WriteLine("!");
                            Console.WriteLine("wrong login data");
                        }
                    }
                    else if (logData[0] == "reg")
                    {
                        if (dc.registerUser(logData[1], logData[2]))
                        {
                            sWriter.WriteLine("1");
                        }
                        else sWriter.WriteLine("!");
                    }
                    else Console.WriteLine("wrong command");
                } while (!logged);

                while (logged)
                {
                    sData = sReader.ReadLine();
                    Console.WriteLine(sData);
                    string[] logData = checkMessage(sData);
                    if (sData == "logout")
                    {
                        Thread.Sleep(2000);
                        logged = false;
                    }
                    else if (logData[0] == "ref")
                    {
                        if (rooms.Count != 0)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("rom ");
                            foreach (Room room in rooms)
                            {

                                string msg = room.id + " " + room.isPrivate + " " + room.activeUsers + " " + room.begun + " / ";

                                sb.Append(msg);
                                if (room == rooms.Last())
                                {
                                    sb.Append(" end");
                                }
                                sWriter.Write(sb);
                            }
                        }
                        else sWriter.Write("     end");
                    }
                    else if (logData[0] == "crm")
                    {
                        rooms.Add(new Room(rooms.Count, bool.Parse(logData[1]), logData[2]));
                        sWriter.WriteLine(rooms[rooms.Count - 1].id);
                    }
                    else if (logData[0] == "jrm")
                    {
                        rooms[int.Parse(logData[1])].join(Guid.Parse(logData[2]), logData[3]);
                    }
                    else if (logData[0] == "lrm")
                    {
                        rooms[int.Parse(logData[1])].leave(Guid.Parse(logData[2]));
                    }
                }
            }
        }
        private string[] checkMessage(string sData)
        {
            return sData.Split(' ');
        }

    }
}
