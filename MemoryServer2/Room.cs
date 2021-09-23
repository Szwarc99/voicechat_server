using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryServer2
{

    class Room
    {
        public int id;
        private string password;
        public bool isPrivate;
        Thread listenerThread;
        public Dictionary<IPEndPoint, List<byte[]>> users = new Dictionary<IPEndPoint, List<byte[]>>();

        UdpClient udpServer = new UdpClient(8100);
        public Room(int id, bool isPrivate, string password)
        {
            this.isPrivate = isPrivate;
            this.password = password;
            this.id = id;
            listenerThread = new Thread(unused =>
            {
                ReceiveUDP();
            });
            listenerThread.Start();
        }
        public string Password
        {
            get { return password; }
            set { password = ""; }
        }

        void StartNewListener()
        {
            Console.WriteLine("Starting new UDP listener");

        }
        public void ReceiveUDP()
        {
            while (true)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 9100);
                    var data = udpServer.Receive(ref remoteEP);
                    Console.WriteLine(remoteEP.ToString() + ": " + data.Length);

                    if (!users.ContainsKey(remoteEP))
                    {
                        List<byte[]> buffer = new List<byte[]>();
                        users.Add(remoteEP, buffer);
                    }
                    else
                    {
                        List<byte[]> newBuff = users[remoteEP];
                        newBuff.Add(data);
                        users[remoteEP] = newBuff;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }


        public void HandleClient(TcpClient client, string playerID, string password)
        {
            NetworkStream stream = client.GetStream();

            if (this.password == password)
            {

            }
            else
            {
                string error = "error wrong_password";
                CommProtocol.Write(stream, error);
                return;
            }
            CommProtocol.Write(stream, "ok");

            bool inRoom = true;

            while (inRoom)
            {
                string sData = "";
                try
                {
                    sData = CommProtocol.Read(stream);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("Disconnecting the player " + playerID);
                    sData = "lrm";
                }
                if (sData != "noop") { Console.WriteLine(sData); }

                string[] logData = CommProtocol.CheckMessage(sData);

                if (sData == "noop")
                {

                }
                else if (logData[0] == "lrm")
                {
                    inRoom = false;
                }
            }
        }
        public string Encode()
        {
            return " " + id + " " + isPrivate;
        }
    }
}
