namespace InternetTime.Custom.SuperColliderZeugs {
    using System;
    using System.Buffers.Binary;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using OSCData;
    using UnityEngine;
    
    public class OscTcpClient {
        public event Action<OSCMessage, IPEndPoint> OnReceive;
        public event Action OnConnected;

        private bool connected;

        private volatile TcpClient server;
        private IPEndPoint serverEndpoint;
        private Thread receiveThread;


        public void Connect(IPEndPoint serverEndpoint) {
            if (server != null) return;
            server = new TcpClient();
            //boundAddress = (IPEndPoint) server.Client.LocalEndPoint;
            this.serverEndpoint = serverEndpoint;
            server.BeginConnect(serverEndpoint.Address, serverEndpoint.Port, OnConnectionSuccessful, server);
        }

        public void Disconnect() {
            if (server == null) return;
            this.connected = false;
            server.Dispose();
            receiveThread.Join();
            Debug.Log("Post Join");
            server = null;
        }

        public void Send(OSCMessage message) {
            Debug.Log("Entered Send");
            byte[] messageAsBytes = message.ToByteArray();
            Span<byte> messageLengthSpan = stackalloc byte[sizeof(int)];
            Debug.Log("Pre get stream");
            NetworkStream stream = server.GetStream();
            Debug.Log("Can Write " + stream.CanWrite);

            BinaryPrimitives.WriteInt32BigEndian(messageLengthSpan, messageAsBytes.Length);

            stream.Write(messageLengthSpan);
            stream.Write(messageAsBytes);
        }
        
        private void OnConnectionSuccessful(IAsyncResult result) {
            server.EndConnect(result);
            this.connected = true;
            receiveThread = new Thread(ReceiveMessage);
            receiveThread.Start();
            Debug.Log("Invoking OnConnected");
            OnConnected?.Invoke();
            // ReceiveMessage();
        }

        private void ReceiveMessage() {
            NetworkStream stream = server.GetStream();
            Span<byte> messageLengthArr = stackalloc byte[sizeof(int)];
            while (connected) {
                
                int messageLengthRead = stream.Read(messageLengthArr);
                Debug.Log("Read Message with Length: " + messageLengthRead);
                int messageLength = BinaryPrimitives.ReadInt32BigEndian(messageLengthArr);

                byte[] data = new byte[messageLength];
                int messageNumBytes = stream.Read(data, 0, messageLength);
                Debug.Log("Read Message Num Bytes: " + messageNumBytes);
                if (messageNumBytes > 0) {
                    OSCMessage receivedMessage = (OSCMessage) OSCPacket.FromByteArray(data);
                    Debug.Log("Message Address: " + receivedMessage.Address);
                    OnReceive?.Invoke(receivedMessage, serverEndpoint);   
                }
            }
        }

        /*
        private void OnTcpRe(IAsyncResult result) {
            UdpClient activeserver = (UdpClient)result.AsyncState!;
            if (server != activeserver) return;

            IPEndPoint source = new IPEndPoint(IPAddress.Any, 0);
            byte[] receivedData = activeserver.EndReceive(result, ref source);
            if (source != null && receivedData.Length > 0) {
                OSCMessage message = (OSCMessage) OSCPacket.FromByteArray(receivedData);
                OnReceive?.Invoke(message, source);
            }
            server?.BeginReceive(OnUdpReceive, result.AsyncState);
        }
        */
    }
}
