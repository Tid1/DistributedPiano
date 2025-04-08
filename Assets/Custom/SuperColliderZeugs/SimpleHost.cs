namespace InternetTime.Custom.SuperColliderZeugs {
    using System;
    using System.Net;

    public class SimpleHost {
        public string LobbyName { get; set; }
        public TimeSpan RTT { get; set; }
        public IPEndPoint UdpEndpoint { get; }
        public IPEndPoint TcpEndpoint { get; }
        public IPEndPoint BeaconEndpoint { get;}

        public SimpleHost(string lobbyName, IPEndPoint udpEndpoint, IPEndPoint tcpEndpoint, IPEndPoint beaconEndpoint) {
            this.LobbyName = lobbyName;
            this.UdpEndpoint = udpEndpoint;
            this.TcpEndpoint = tcpEndpoint;
            this.BeaconEndpoint = beaconEndpoint;
        }

        public override bool Equals(object obj) {
            if (obj is not SimpleHost other) return false;
            return this.UdpEndpoint == other.UdpEndpoint &&
                   this.TcpEndpoint == other.TcpEndpoint &&
                   this.BeaconEndpoint == other.BeaconEndpoint;
        }

        public override int GetHashCode() {
            return HashCode.Combine(UdpEndpoint, TcpEndpoint, BeaconEndpoint);
        }
    }
}
