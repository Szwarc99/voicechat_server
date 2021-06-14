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
        RSAParameters RSAKeyInfo;




        private List<string> loggedUsers = new List<string>();       

        private ConcurrentDictionary<int, Room> roomDict = new ConcurrentDictionary<int, Room>();
        private int nextRoomId = 0;

        public ServerTCP(int port)
        {
            _server = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            _server.Start();            
            _isRunning = true;
            rsa = rsa = new RSACryptoServiceProvider(1024);
            var privKString = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RSAParameters xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Exponent>AQAB</Exponent>\r\n  <Modulus>4HAXJZl0crjN0xZ+UFqe+N/ptG3K2cSrCvKXokL08heM+fwyvw53OemwCtsmFD6yn6YOYwrvRQ/ekQztM/t6/9pK0KxenDQDDB59oLxXYGZ437kzTIvC2kHehrLsdpwuW54wp0KnF4K03ABYe5oJyg3JsLust/OJLadi70WF4mE=</Modulus>\r\n  <P>5df0Ppx+0TLrDhIawuHJIdDIYk2GfvwHaSYILM5uubVG4OhECm0VBf4yC36mMnToxqtvmJH3wX9zs7hVN8ZESw==</P>\r\n  <Q>+fqmuQ4Y5qL235BMr24GL+fEdKbL4vxv/M45+iAOJC9JKi/fKwAB+h9KJSMfoR9JP1GmjPhhMFln+FCGpjzQgw==</Q>\r\n  <DP>o9q1m+EzI256VfigLWiLW9kc0b/U7zg7DEH5t/+evjO2iOXsg8ZKI5CZGsq6LuRbgi57izgceUykLm5uCioFSw==</DP>\r\n  <DQ>BX4ca7SDl429Huxswu4H9MWC640+rZ4eV8+wNm694M2pLeQfYzJ82KIXXvmGmGO3mEyS/EX43LcaMbqTOtPbQQ==</DQ>\r\n  <InverseQ>mGuyHBrz9+ySbKTLlWscA2IuHyDBBaRp7zq4bEwjioEfUhJy1NqE2DTDyzGmXzfBg7KGj0U/hhIAg6sEYZJ1FQ==</InverseQ>\r\n  <D>YMaUhIb12l3rimDBmJ5qu/+5Ay7wcBRIeJEAZ1wdyKH1DPn9W7q+GD+2xAeZFNOwK/zraTOW1p2wJ7V+NpLyhr9QxD+l++UbiHzGmp5cQufCwjqCU1ZfyV7wVhxTKDQwF9LWmGInp36oDQ1AyILJxANg6ytfqL3OrJsITSOuGD0=</D>\r\n</RSAParameters>";

            var sr = new System.IO.StringReader(privKString);
            //we need a deserializer
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            //get the object back from the stream
            var privKey = (RSAParameters)xs.Deserialize(sr);
            rsa.ImportParameters(privKey);


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
