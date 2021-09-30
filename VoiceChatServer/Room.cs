using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

namespace VoiceChatServer
{
    class Client
    {
        public int offset;
        public double avgTimeAhead;
        public double missedFactor;
        public Dictionary<int, byte[]> buffer = new Dictionary<int, byte[]>();
    }

    public static class WinApi
    {
        /// <summary>TimeBeginPeriod(). See the Windows API documentation for details.</summary>

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1401:PInvokesShouldNotBeVisible"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressUnmanagedCodeSecurity]
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]

        public static extern uint TimeBeginPeriod(uint uMilliseconds);

        /// <summary>TimeEndPeriod(). See the Windows API documentation for details.</summary>

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1401:PInvokesShouldNotBeVisible"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressUnmanagedCodeSecurity]
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]

        public static extern uint TimeEndPeriod(uint uMilliseconds);
    }

    class Room
    {
        public int id;
        private string password;
        public bool isPrivate;
        public Dictionary<SocketAddress, string> usernames = new Dictionary<SocketAddress, string>();
        Thread listenerThread;
        Thread mixerThread;
        int current = 0;
        Stopwatch stopwatch = new Stopwatch();
        public Dictionary<SocketAddress, Client> users = new Dictionary<SocketAddress, Client>();
        UdpClient udpServer;
        public Room(int id, bool isPrivate, string password)
        {
            this.isPrivate = isPrivate;
            this.password = password;
            this.id = id;
            udpServer = new UdpClient(8100 + id);
            listenerThread = new Thread(unused =>
            {
                ReceiveUDP();
            });
            listenerThread.Start();

            mixerThread = new Thread(unused =>
            {
                SendAudioBack();
            });
            mixerThread.Start();
        }
        public string Password
        {
            get { return password; }
            set { password = ""; }
        }
        public void ReceiveUDP()
        {
            while (true)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    var data = udpServer.Receive(ref remoteEP);
                    byte[] audio = new byte[320];
                    Array.Copy(data, 4, audio, 0, 320);
                    var index = BitConverter.ToInt32(data, 0);

                    Console.WriteLine(remoteEP.ToString() + ": " + data.Length);
                    SocketAddress sa = remoteEP.Serialize();

                    lock (this)
                    {
                        if (!users.ContainsKey(sa))
                        {
                            Client client = new Client();                            
                            client.offset = current - index;
                            client.avgTimeAhead = current * 10 - stopwatch.ElapsedMilliseconds;
                            users.Add(sa, client);                            
                        }
                        int targetFrame = users[sa].offset + index;
                        double timeAhead = Math.Max(-50, targetFrame * 10 - stopwatch.ElapsedMilliseconds);
                        users[sa].missedFactor = 0.99 * users[sa].missedFactor
                            + (timeAhead < 0 ? 0.01 : 0.0);
                        users[sa].avgTimeAhead = 0.99 * users[sa].avgTimeAhead + 0.01 * timeAhead;                        

                        if (users[sa].missedFactor > 0.1)
                        {
                            users[sa].offset = current - index;
                            targetFrame = current;
                            users[sa].missedFactor *= 0.5;
                        }
                        if (users[sa].avgTimeAhead > 30.0)
                        {
                            users[sa].offset--;
                            targetFrame--;
                            users[sa].avgTimeAhead -= 10.0;
                        }
                        if (targetFrame >= current)
                        {
                            users[sa].buffer[targetFrame] = audio;
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public void SendAudioBack()
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.RealTime;
            stopwatch.Start();
            while (true)
            {
                long nextTime = current * 10;
                WinApi.TimeBeginPeriod(1);             
                while (stopwatch.ElapsedMilliseconds <= nextTime)
                {                    
                    Thread.Sleep(1);                    
                }                
                WinApi.TimeEndPeriod(1);
                
                lock (this)
                {
                    var keys = users.Keys;
                    foreach (var key in keys)
                    {
                        List<Pcm16BitToSampleProvider> sources = new List<Pcm16BitToSampleProvider>();
                        foreach (var k in keys)
                        {
                            if (key != k &&
                                users[k].buffer.TryGetValue(current, out byte[] sample))
                            {
                                var ms = new MemoryStream(sample);
                                var rs = new RawSourceWaveStream(ms, new WaveFormat(16000, 16, 1));
                                var r = new Pcm16BitToSampleProvider(rs);                                
                                sources.Add(r);                                
                            }
                        }
                        if (sources.Count != 0)
                        {                            
                            var mixer = new MixingSampleProvider(sources);
                            byte[] index = BitConverter.GetBytes(current);
                            byte[] data = new byte[324];
                            Array.Copy(index, data, 4);
                            mixer.ToWaveProvider16().Read(data, 4, 320);
                            IPEndPoint ep = new IPEndPoint(0, 0);
                            ep = (IPEndPoint)ep.Create(key);
                            udpServer.Send(data, data.Length, ep);
                        }
                    }

                    int summedBuffers = 0;
                    foreach (var key in keys)
                    {
                        summedBuffers = users[key].buffer.Count;
                        users[key].buffer.Remove(current);                        
                    }
                    if (users.Count == 0)
                    {
                        current = 0;
                        stopwatch.Restart();
                    }
                    else
                    {
                        current++;
                    }

                }
            }
        }

        public void HandleClient(TcpClient client, string playerID, string password, int udpPort)
        {
            NetworkStream stream = client.GetStream();

            if (this.password == password)
            {
                IPEndPoint epp = (IPEndPoint)client.Client.RemoteEndPoint;
                epp.Port = udpPort;
                var saa = epp.Serialize();
                lock (this)
                { 
                    usernames.Add(saa, playerID);
                }
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
                    Console.WriteLine("User " + playerID + " is leaving room "+ id);
                    sData = "lrm";
                }
                if (sData != "noop") { Console.WriteLine(sData); }

                string[] logData = CommProtocol.CheckMessage(sData);

                if (sData == "pull")
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("pull");
                    sb.Append(" " + usernames.Count);
                    foreach (var p in usernames)
                    {
                        sb.Append(" " + p.Value);
                    }
                    CommProtocol.Write(stream,sb.ToString());
                }
                else if (logData[0] == "lrm")
                {                    
                    inRoom = false;
                }
            }
            IPEndPoint ep = (IPEndPoint)client.Client.RemoteEndPoint;
            ep.Port = udpPort;
            var sa = ep.Serialize();
            lock (this)
            {
                users.Remove(sa);
                usernames.Remove(sa);
            }
        }
        public string Encode()
        {
            return " " + id + " " + isPrivate;
        }
    }
}
