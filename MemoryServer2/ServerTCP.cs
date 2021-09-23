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

namespace MemoryServer2
{
    class ServerTCP
    {
        private TcpListener _server;
        private Boolean _isRunning;

        private List<string> loggedUsers = new List<string>();

        private ConcurrentDictionary<int, Room> roomDict = new ConcurrentDictionary<int, Room>();
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
                        Console.WriteLine(e);
                        Console.WriteLine("Client has disconnected due to error");
                        newClient.Close();
                    }
                });
                t.Start();
            }
        }

        static string Hash(string password)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        public void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            CommProtocol.setAes(stream);


            // sets two streams
            //StreamWriter sWriter = new StreamWriter(client.GetStream(), Encoding.ASCII);
            //StreamReader sReader = new StreamReader(client.GetStream(), Encoding.ASCII);
            // you could use the NetworkStream to read and write, 
            // but there is no forcing flush, even when requested

            bool clientConnected = true;
            /*DatabaseConnector dc = new DatabaseConnector();
            bool logged = false;
            string playerID = "";*/

            CommProtocol.Write(stream, "test");

            while (clientConnected)
            {
                Console.WriteLine("loop");
                string userID;
                string sData = "";

                try
                {
                    sData = CommProtocol.Read(stream);
                    Console.WriteLine(sData);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    sData = "logout";
                }
                string[] logData = CommProtocol.CheckMessage(sData);

                var rooms = this.roomDict.ToArray().Select(x => x.Value).ToList();
                rooms.Sort((x, y) => x.id - y.id);

                if (sData == "logout")
                {
                    clientConnected = false;
                }
                if (logData[0] == "user")
                {
                    userID = logData[1];
                    lock (loggedUsers)
                    {
                        loggedUsers.Add(userID);
                    }
                    CommProtocol.Write(stream, "ok");
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
                /*else if (logData[0] == "chngpass")
                {
                    dc.editUserPassword(logData[1], Hash(logData[1] + logData[2]));
                    CommProtocol.Write(stream, "ok");
                }
                */
            }
        }
    }
}
