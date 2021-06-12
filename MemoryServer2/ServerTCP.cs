using System;
using System.Collections.Concurrent;
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

        private ConcurrentDictionary<int, Room> roomDict = new ConcurrentDictionary<int, Room>();
        private int nextRoomId = 0;

        public ServerTCP(int port)
        {
            _server = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            _server.Start();

            _isRunning = true;
            
            LoopClients();
        }
        private int getNextId()
        {
            return Interlocked.Increment(ref nextRoomId)-1;
        }

        public void LoopClients()
        {            
            while (_isRunning)
            {
                // wait for client connection
               // object[] vs = new object[2];
                TcpClient newClient = _server.AcceptTcpClient();
                //vs[0] = newClient;
                //vs[1] = rooms;
                // client found.
                // create a thread to handle communication

                Thread t = new Thread(unused => HandleClient(newClient)
  );
                t.Start();
            }
        }

        public void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            // sets two streams
            //StreamWriter sWriter = new StreamWriter(client.GetStream(), Encoding.ASCII);
            //StreamReader sReader = new StreamReader(client.GetStream(), Encoding.ASCII);
            // you could use the NetworkStream to read and write, 
            // but there is no forcing flush, even when requested

            Boolean bClientConnected = true;
            String sData = null;
            DatabaseConnector dc = new DatabaseConnector();
            bool logged = false;
            

            while (bClientConnected)
            {
                do
                {
                    
                    sData = CommProtocol.read(stream);
                    Console.WriteLine(sData);
                    string[] logData = checkMessage(sData);
                    if (logData[0] == "log")
                    {
                        if (dc.checkUserData(logData[1], logData[2]))
                        {

                            Console.WriteLine("user logged");
                            CommProtocol.write(stream, "1");
                            logged = true;
                        }
                        else
                        {
                            CommProtocol.write(stream, "!");
                            Console.WriteLine("wrong login data");
                        }
                    }
                    else if (logData[0] == "reg")
                    {
                        if (dc.registerUser(logData[1], logData[2]))
                        {
                            CommProtocol.write(stream, "1");
                        }
                        else CommProtocol.write(stream, "!");
                    }
                    else Console.WriteLine("wrong command");
                } while (!logged);

                while (logged)
                {
                    sData = CommProtocol.read(stream);
                    Console.WriteLine(sData);
                    string[] logData = checkMessage(sData);

                    var rooms = this.roomDict.ToArray().Select(x => x.Value).ToList();
                    rooms.Sort((x, y) => x.id - y.id);

                    if (sData == "logout")
                    {
                        Thread.Sleep(2000);
                        logged = false;
                        bClientConnected = false;
                    }
                    else if (logData[0] == "ref")
                    {                        
                        if (rooms.Count != 0)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append(rooms.Count);
                            foreach (Room room in rooms)
                            {

                                string msg =" "+room.id + " " + room.isPrivate + " " + room.activeUsers + " " + room.begun + " /";

                                sb.Append(msg);                               
                            }                            
                            CommProtocol.write(stream, sb.ToString());
                            Console.WriteLine(((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString()+ " " + sb);


                        }
                        else CommProtocol.write(stream, "~");
                    }
                    else if (logData[0] == "crm")
                    {
                        int id = getNextId();
                        roomDict.TryAdd(id, new Room(id, bool.Parse(logData[1]), logData[2]));
                        CommProtocol.write(stream, id.ToString());
                    }
                    else if (logData[0] == "jrm")
                    {
                        rooms[int.Parse(logData[1])].join(logData[2], logData[3]);
                        write("ok");
                    }
                    else if (logData[0] == "lrm")
                    {
                        rooms[int.Parse(logData[1])].leave(logData[2]);
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
