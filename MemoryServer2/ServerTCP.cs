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
            return Interlocked.Increment(ref nextRoomId) - 1;
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

                Thread t = new Thread(unused =>
                {
                    try
                    {
                        HandleClient(newClient);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Client has disconnected due to error");
                    }
                });
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

            bool clientConnected = true;
            DatabaseConnector dc = new DatabaseConnector();
            bool logged = false;
            string playerID = "";


            while (clientConnected)
            {
                do
                {
                    string sData = CommProtocol.Read(stream);
                    Console.WriteLine(sData);
                    string[] logData = CommProtocol.CheckMessage(sData);
                    if (logData[0] == "log")
                    {
                        if (dc.checkUserData(logData[1], logData[2]))
                        {

                            Console.WriteLine("user logged");
                            CommProtocol.Write(stream, "1");
                            logged = true;
                            playerID = logData[1];
                        }
                        else
                        {
                            CommProtocol.Write(stream, "!");
                            Console.WriteLine("wrong login data");
                        }
                    }
                    else if (logData[0] == "reg")
                    {
                        if (dc.registerUser(logData[1], logData[2]))
                        {
                            CommProtocol.Write(stream, "1");
                        }
                        else CommProtocol.Write(stream, "!");
                    }
                    else Console.WriteLine("wrong command");
                } while (!logged);

                while (logged)
                {
                    string sData = "";
                    try
                    {
                        sData = CommProtocol.Read(stream);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Console.WriteLine("Logging out player " + playerID +" due to error");
                        //TODO logout
                        sData = "logout";
                    }
                    Console.WriteLine(sData);
                    string[] logData = CommProtocol.CheckMessage(sData);

                    var rooms = this.roomDict.ToArray().Select(x => x.Value).ToList();
                    rooms.Sort((x, y) => x.id - y.id);

                    if (sData == "logout")
                    {
                        Thread.Sleep(2000);
                        logged = false;
                        clientConnected = false;
                    }
                    else if (logData[0] == "ref")
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(rooms.Count);
                        foreach (Room room in rooms)
                        {
                            sb.Append(room.Encode());
                        }
                        CommProtocol.Write(stream, sb.ToString());
                        Console.WriteLine(((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString() + " " + sb);
                    }
                    else if (logData[0] == "crm")
                    {
                        int id = getNextId();
                        roomDict.TryAdd(id, new Room(id, bool.Parse(logData[1]), logData[2]));
                        CommProtocol.Write(stream, id.ToString());
                    }
                    else if (logData[0] == "jrm")
                    {
                        string pwd = "";
                        if (logData.Length == 4)
                        {
                            pwd = logData[3];
                        }
                        rooms[int.Parse(logData[1])].HandleClient(client, logData[2], pwd);
                    }
                }
            }
        }
    }
}
