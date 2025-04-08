namespace InternetTime.Custom.SuperColliderZeugs {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using InternetTime.Custom.SuperColliderZeugs;
    using OSCData;
    using UnityEngine;

    public delegate void StartMusicHandler();
    public delegate void OctaveConfigHandler(int numOfOctaves, int startOctave);

    public delegate void ReceiveRTTHandler(long rtt);
    public delegate void HostInformationHandler(List<string> lobbies);
    public delegate void ReceiveMidiHandler(string fileName, byte[] data);


    public class GameNetwork {
        public event OctaveConfigHandler OnReceiveOctaveConfig;
        public event StartMusicHandler OnStartMusic;
        public event HostInformationHandler OnHostsChanged;
        public event ReceiveMidiHandler OnReceiveMidi;
        public event ReceiveRTTHandler OnRTTReceived;

        public event Action OnInfoSent;
        
        private readonly string DEVICE_NAME = SystemInfo.deviceName;


        private OscUdpClient oscUdpClient = new OscUdpClient(IPAddress.Any);
        private OscDiscoveryClient discoveryClient = new OscDiscoveryClient();
        private OscTcpClient oscTcpClient = new OscTcpClient();
        private Dictionary<string, Action<OSCMessage, IPEndPoint>> routes = new();

        private Dictionary<IPEndPoint, SimpleHost> availableHosts = new();

        private SimpleHost host;

        public GameNetwork() {
            routes.Add("/config", ReceiveOctaveConfig);
            routes.Add("/start", ReceiveStartSignal);
            routes.Add("/midi/init", ReceiveMidiFile);
            routes.Add("/rtt/request", ReceiveRTT);
            
            discoveryClient.OnReceive += ReceiveHostInformation;
            //discoveryClient.OnDeath += UpdateAvailableHosts;
            oscUdpClient.OnReceive += ReceiveMessage;
            oscTcpClient.OnConnected += SendJoinInfo;
            oscTcpClient.OnReceive += ReceiveMessage;
            
            
            discoveryClient.Start();
            oscUdpClient.Start();
        }

        public void Stop() {
            Debug.Log("Network Stop 1");
            discoveryClient.Stop();
            Debug.Log("Network Stop 2");
            oscUdpClient.Stop();
            Debug.Log("Network Stop 3");
            oscTcpClient.Disconnect();
            Debug.Log("Network Stop 4");
        }

        public void SendScore(float score) {
            OSCMessage message = new OSCMessage("/point");
            message.Append(score);

            oscUdpClient.Send(message, host.UdpEndpoint);
        }

        public void SendKeyOn(int key) {
            OSCMessage message = new OSCMessage("/keyOn");
            message.Append(key);

            oscUdpClient.Send(message, host.UdpEndpoint);
        }

        public void SendKeyOff(int key) {
            OSCMessage message = new OSCMessage("/keyOff");
            message.Append(key);

            oscUdpClient.Send(message, host.UdpEndpoint);
        }

        public void SendKeyOnDebug(int key, float velocity, float startTime) {
            OSCMessage message = new OSCMessage("/keyOn");
            message.Append(key);
            message.Append(velocity);
            message.Append(startTime);
            
            oscUdpClient.Send(message, host.UdpEndpoint);
        }

        public void SendKeyOffDebug(int key, float endTime, long elapsedMs, long elapsedTicks) {
            OSCMessage message = new OSCMessage("/keyOff");
            message.Append(key);
            message.Append(endTime);
            message.Append((int)elapsedMs);
            message.Append((int)elapsedTicks);
            
            oscUdpClient.Send(message, host.UdpEndpoint);
        }

        public void SendJoinReq(string lobby) {
            foreach (SimpleHost potentialHost in availableHosts.Values) {
                if (potentialHost.LobbyName == lobby) {
                    host = potentialHost;
                    oscTcpClient.Connect(host.TcpEndpoint);
                }
            }
        }

        public void SendJoinInfo() {
            Debug.Log("Sending Join...");
            OSCMessage message = new OSCMessage("/join");
            message.Append(DEVICE_NAME);
            message.Append(oscUdpClient.BoundPort);
            Debug.Log("About to send message...");
            oscTcpClient.Send(message);
            OnInfoSent?.Invoke();
        }

        public void SendRTTResponse(int sequenceCounter) {
            OSCMessage message = new OSCMessage("/rtt/response");
            message.Append(sequenceCounter);
            oscUdpClient.Send(message, host.UdpEndpoint);
        }
        
        private void ReceiveMessage(OSCMessage message, IPEndPoint endPoint) {
            string address = message.Address;
            if (address != null && routes.TryGetValue(address, out var action)) {
                action?.Invoke(message, endPoint);
            }
        }

        private void ReceiveHeartbeat(OSCMessage message, IPEndPoint endPoint) {
            OSCMessage response = new OSCMessage("/heartbeat/response");
            oscTcpClient.Send(response);
        }

        private void ReceiveOctaveConfig(OSCMessage message, IPEndPoint endPoint) {
            int numOfOctaves = (int) message.Data[0];
            int startOctave = (int) message.Data[1];

            OnReceiveOctaveConfig?.Invoke(numOfOctaves, startOctave);
        }

        private void ReceiveMidiFile(OSCMessage message, IPEndPoint endPoint) {
            Debug.Log("ReceiveMidiFile Entered");
            string fileName = (string) message.Data[0];
            byte[] data = (byte[]) message.Data[1];

            OnReceiveMidi?.Invoke(fileName, data);
        }


        private void ReceiveStartSignal(OSCMessage message, IPEndPoint endPoint) {
            //long time = (long) message.Data[0];

            OnStartMusic?.Invoke();
        }

        private void ReceiveRTT(OSCMessage message, IPEndPoint endPoint) {
            long rtt = (long)message.Data[0];
            int sequenceCounter = (int) message.Data[1];
            OnRTTReceived?.Invoke(rtt);
            SendRTTResponse(sequenceCounter);
        }

        //Route:/discovery/promotion
        private void ReceiveHostInformation(OSCMessage message, IPEndPoint endPoint) {
            int hostUdpPort = (int) message.Data[0];
            int hostTcpPort = (int) message.Data[1];
            string lobbyName = (string) message.Data[2];

            lock (availableHosts) {

                if (availableHosts.ContainsKey(endPoint)) return;

                IPEndPoint hostUdpEndpoint = new IPEndPoint(endPoint.Address, hostUdpPort);
                IPEndPoint hostTcpEndpoint = new IPEndPoint(endPoint.Address, hostTcpPort);

                availableHosts[endPoint] = new SimpleHost(lobbyName, hostUdpEndpoint, hostTcpEndpoint, endPoint);
                Debug.Log("Lobby name: " + lobbyName);
                OnHostsChanged?.Invoke(availableHosts.Select(x => x.Value.LobbyName).ToList());
            }

        }

        //TODO rework update mechanic
        private void UpdateAvailableHosts(IPEndPoint deadBeacon) {
            lock (availableHosts) {
                availableHosts.Remove(deadBeacon);
                OnHostsChanged?.Invoke(availableHosts.Select(x => x.Value.LobbyName).ToList());
            }
        }
    }
}
