using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MemoryServer2
{
    class ServerTCP
    {
        TcpListener MyListener;
        TcpClient MyClient;
        NetworkStream stream;
        IPAddress ip;
        int port;

        #region getters&setters
        public TcpListener Listener { get => MyListener; set => MyListener = value; }
        public TcpClient TcpClient { get => MyClient; set => MyClient = value; }
        protected NetworkStream Stream { get => stream; set => stream = value; }
        public IPAddress IP { get => ip; set => ip = value; }
        public int Port { get => port; set => port = value; }
        #endregion getters&setters


        public ServerTCP(IPAddress ip, int port)
        {
            IP = ip;
            Port = port;
            Listener = new TcpListener(ip, port);
        }

        public delegate void TransmissionDataDelegate(NetworkStream stream);

        protected void StartListening()
        {
            Listener = new TcpListener(ip, Port);
            Listener.Start();
        }

        protected void AcceptClient()
        {
            Menu menu = new Menu();
            while (true)
            {
                TcpClient tcpClient = MyListener.AcceptTcpClient();
                Stream = tcpClient.GetStream();
                TransmissionDataDelegate transmissionDelegate = menu.BeginDataTransmission;
                transmissionDelegate.BeginInvoke(Stream, TransmissionCallback, tcpClient);
            }
        }
        private void TransmissionCallback(IAsyncResult ar)
        {
            TcpClient client = (TcpClient)ar.AsyncState;
            client.Close();
        }

        public void Start()

        {
            StartListening();
            AcceptClient();
        }




    }
}
