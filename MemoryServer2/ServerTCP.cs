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
    class UserInfo
    { 

    }
    class ServerTCP
    {
        private TcpListener _server;
        private Boolean _isRunning;

        private RSACryptoServiceProvider rsa;
        private string privKey;




        private List<string> loggedUsers = new List<string>();       

        private ConcurrentDictionary<int, Room> roomDict = new ConcurrentDictionary<int, Room>();
        private int nextRoomId = 0;

        public ServerTCP(int port)
        {
            _server = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            _server.Start();
            privKey = "MIICXAIBAAKBgF82iEMWuHk0JaFUGVUQ7DqXbzg2VAo/U9u4xtD8Z7rOQbXjBsROlBvOMUaa0ztdTPhTByfIv4PBjN0pis6p/Bpdwlyz4RyvRDARhnaFGEJ1VTrrc0G+buRGYKin1nd/1KPvDwhgARD+NM3Mta//M4FcXgnNJ2WVEsY7Vh92BvTTAgMBAAECgYBUzVox/tORSEvX0/K4HFl6mhQ6SdEyS1MiWQHjc1vkOv61xJ3rTF2IIm8rBozqy9/ZMQInghppfIM9HFoAVdAuiV7kvDv9lvyswiWaOe+fA7MQ73yP5O8ofzy41XkoSFqDjdZB0Tzml/CKmB/f737WoaBFzyxkp9U141eL6MOAYQJBAKFdaCZe4211X5ZBZojE1m8bSaVHYD/fgLVUe3K74h6x6vGjSeFmuvFP0HM44X65gBiN6EfrrLYijWFdSWP6AxECQQCXDWJ/kU+2Tzrd//1jQ6Rti5SKI7Dk0Vpa1IDyqPD5LmyWu+dfMYWnZQ0HsQFlkRGpQE62qgCYIHZI0vW5tPGjAkEAlpITWiKWsw+f9xPlul95/EkJKll03YUPk6RWYNQihiPcqEeG6/WxIPUp/Coqd9ZeSgs4oMuv6HBLXnvuvISREQJBAIm/rgRxioTR6fgLe5KrW+Z+NH5pH+b7N+++/LzN7br/aA1p3AyGh8DouSI7e++YhMeZGm8fxxzz9Yphv66T4QsCQEqZcP8myZTYxibeFa47q/3PjQD4EbTT/8XLZEl+AEKl7xJz1TSbotUxwZ146rj7R2H0ew7PVlRd0y092e/VMVE=";
            _isRunning = true;
            rsa = new RSACryptoServiceProvider(1024);            
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
                        if (!loggedUsers.Contains(logData[1]))
                        {
                            if (dc.checkUserData(logData[1], logData[2]))
                            {

                                Console.WriteLine("user logged");
                                CommProtocol.Write(stream, "1");
                                logged = true;
                                playerID = logData[1];
                                lock(loggedUsers)
                                {
                                    loggedUsers.Add(playerID);
                                }
                            }
                            else
                            {
                                CommProtocol.Write(stream, "!");
                                Console.WriteLine("wrong login data");
                            }
                        }
                        else CommProtocol.Write(stream,"error already_logged_in");
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
                        logged = false;
                        clientConnected = false;
                        lock(loggedUsers)
                        { 
                            loggedUsers.Remove(playerID);
                        }
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
