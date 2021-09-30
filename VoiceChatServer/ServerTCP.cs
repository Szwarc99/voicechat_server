using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceChatServer
{
    class ServerTCP
    {
        private TcpListener _server;
        private Boolean _isRunning;

        private HashSet<string> loggedUsers = new HashSet<string>();

        private ConcurrentDictionary<int, Room> roomDict = new ConcurrentDictionary<int, Room>();
        private ConcurrentDictionary<string, int> roomOwners = new ConcurrentDictionary<string, int>();
        private int nextRoomId = 0;

        public ServerTCP(int port)
        {
            _server = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            _server.Start();
            _isRunning = true;
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
                TcpClient newClient = _server.AcceptTcpClient();
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
                        Console.WriteLine(e);
                        Console.WriteLine("Client has disconnected due to error");
                        newClient.Close();
                    }
                });
                t.Start();
            }
        }

        public void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            CommProtocol.setAes(stream);

            bool clientConnected = true;
            bool logged = false;
            string userID = "";

            CommProtocol.Write(stream, "test");
            while (clientConnected)
            {

                do
                {
                    string sData = CommProtocol.Read(stream);
                    Console.WriteLine(sData);
                    string[] logData = CommProtocol.CheckMessage(sData);
                    if (logData[0] == "con")
                    {
                        userID = logData[1];
                        lock (loggedUsers)
                        {
                            if (loggedUsers.Contains(userID))
                            {
                                CommProtocol.Write(stream, "error username_already_taken");
                            }
                            else
                            {
                                logged = true;
                                loggedUsers.Add(userID);
                                CommProtocol.Write(stream, "con ok");
                            }
                        }
                    }
                    else Console.WriteLine("wrong command");
                } while (!logged);

                while (logged)
                {
                    string sData = "";
                    try
                    {
                        sData = CommProtocol.Read(stream);
                        Console.WriteLine(sData);
                    }
                    catch (Exception e)
                    {
                        sData = "dsc";
                    }
                    string[] logData = CommProtocol.CheckMessage(sData);

                    var rooms = this.roomDict.ToArray().Select(x => x.Value).ToList();
                    rooms.Sort((x, y) => x.id - y.id);

                    if (sData == "dsc")
                    {
                        clientConnected = false;
                        lock (loggedUsers)
                        {
                            loggedUsers.Remove(userID);
                        }
                    }
                    if (logData[0] == "ref")
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(rooms.Count);
                        foreach (Room room in rooms)
                        {
                            sb.Append(room.Encode());
                        }
                        CommProtocol.Write(stream, sb.ToString());
                    }
                    else if (logData[0] == "crm")
                    {
                        if (!roomOwners.ContainsKey(userID))
                        {
                            int id = getNextId();
                            roomDict.TryAdd(id, new Room(id, bool.Parse(logData[1]), logData[2]));
                            roomOwners.TryAdd(userID, id);
                            string str = "crm " + id.ToString();
                            CommProtocol.Write(stream, str);
                        }
                        else CommProtocol.Write(stream, "error room_already_created");
                    }
                    else if (logData[0] == "jrm")
                    {
                        string pwd = "";
                        int port;

                        if (logData.Length == 5)
                        {
                            pwd = logData[3];
                            port = Int32.Parse(logData[4]);
                        }
                        else
                            port = Int32.Parse(logData[3]);
                        Console.WriteLine(port);
                        rooms[int.Parse(logData[1])].HandleClient(client, logData[2], pwd, port);
                    }
                }
            }
        }
    }
}
