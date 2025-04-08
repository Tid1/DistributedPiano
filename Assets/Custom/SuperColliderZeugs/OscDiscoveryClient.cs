namespace InternetTime.Custom.SuperColliderZeugs {
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using OSCData;
    using UnityEngine;

    public delegate void HeartbeatHandler(IPEndPoint beaconSignal);

    public class OscDiscoveryClient {
        public event Action<OSCMessage, IPEndPoint> OnReceive;
        public event HeartbeatHandler OnDeath;

        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(3);
        private static readonly IPEndPoint DISCOVERY_ENDPOINT = new IPEndPoint(IPAddress.Any, 50001);

        private readonly Dictionary<IPEndPoint, DateTime> availableHosts = new();
        private readonly object lockObj = new object();
        private volatile UdpClient socket;
        private Thread heartbeatThread;

        public void Start() {
            lock (lockObj) {
                if (socket != null) return;
                heartbeatThread = new Thread(CheckHeartbeat);
                socket = new UdpClient(DISCOVERY_ENDPOINT);
                Debug.Log("Started Discovery Client");
                heartbeatThread.Start();
                socket.BeginReceive(OnUdpReceive, socket);
            }
        }

        public void Stop() {
            lock (lockObj) {
                if (socket == null) return;
                UdpClient active = socket;
                socket = null;
                heartbeatThread.Interrupt();
                heartbeatThread.Join();
                active.Dispose();
            }
        }

        private void CheckHeartbeat() {
            Debug.Log("Heartbeat Thread started!");
            while (socket != null) {
                DateTime current = DateTime.Now;
                List<IPEndPoint> unavailableHosts = new();
                lock (availableHosts) {
                    unavailableHosts.AddRange(from entry in availableHosts
                        where current - entry.Value >= TIMEOUT
                        select entry.Key);
                    
                    foreach (var host in unavailableHosts) {
                        availableHosts.Remove(host);
                        OnDeath?.Invoke(host);
                        Debug.Log("Kicked from Lobby: " + host);
                    }
                }

                try {
                    Thread.Sleep(1000);
                } catch (ThreadInterruptedException e) {
                    //NO-OP
                }
            }
        }

        private void OnUdpReceive(IAsyncResult result) {
            UdpClient activeSocket = (UdpClient) result.AsyncState!;
            if (socket != activeSocket) return;

            IPEndPoint source = new IPEndPoint(IPAddress.Any, 0);
            byte[] receivedData = activeSocket.EndReceive(result, ref source);
            
            socket?.BeginReceive(OnUdpReceive, result.AsyncState);
            
            if (source != null && receivedData.Length > 0) {
                OSCMessage message = (OSCMessage) OSCPacket.FromByteArray(receivedData);
                Debug.Log("Received: " + message.Address);

                lock (availableHosts) {
                    availableHosts[source] = DateTime.Now;
                }
                
                OnReceive?.Invoke(message, source);
            }
        }
    }
}
