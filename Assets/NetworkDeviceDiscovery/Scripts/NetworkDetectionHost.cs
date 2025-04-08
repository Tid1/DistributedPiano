using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEditor;
using NetworkDeviceDiscovery;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NetworkDetectionHost : MonoBehaviour
{
    Probe probe = new Probe();
    Device selectedDevice;
    DeviceList beaconList;

    public void chooseHost()
    {
        GameObject.Find("Beacon").GetComponent<Beacon>().ip = GameObject.Find("IPBroadcast").GetComponent<InputField>().text != ""?GameObject.Find("IPBroadcast").GetComponent<InputField>().text:"255.255.255.255";
        probe = new Probe();
        beaconList = new DeviceList(probe);
        probe.Start();
        searchText.SetActive(true);
        hostButton.SetActive(false);
        ips = new List<string>();

        //RefreshLocalUdpListings();
    }
    public const byte SERVER_BROADCAST_CODE = 42;
    public const byte CLIENT_BROADCAST_CODE = 24;

    public const byte BROADCAST_LISTING_REQUEST_1 = 42;
    public const byte BROADCAST_LISTING_REQUEST_2 = 24;
    public const byte BROADCAST_LISTING_REQUEST_3 = 9;

    //public const ushort DEFAULT_PORT = 15937;
    public const ushort DEFAULT_PORT = 9091;

    private static IPAddress[] GetLocalIPs()
    {
        List<IPAddress> ipList = new List<IPAddress>();

        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            switch (nic.NetworkInterfaceType)
            {
                case NetworkInterfaceType.Wireless80211:
                case NetworkInterfaceType.Ethernet:
                    break;
                default:
                    continue;
            }

            if (nic.OperationalStatus != OperationalStatus.Up) continue;

            foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipList.Add(ip.Address);
                }
            }
        }

        return ipList.ToArray();
    }

    private static List<UdpClient> localListingsClientList = new List<UdpClient>();

    private static void CloseLocalListingsClient()
    {
            foreach (UdpClient cachedUdpClient in localListingsClientList)
            {
                cachedUdpClient.Client.Close();
            }
            localListingsClientList.Clear();
    }


    public static bool EndingSession = false;



    /// <summary>
    /// A method to find all of the local UDP servers and clients on the network
    /// </summary>
    public static void RefreshLocalUdpListings(ushort portNumber = DEFAULT_PORT, int responseBuffer = 1000)
    {

        // Initialize the list to hold all of the local network endpoints that respond to the request
        if (LocalEndpoints == null)
            LocalEndpoints = new List<BroadcastEndpoints>();

        foreach (IPAddress ipAddress in GetLocalIPs())
        {
            // Create a client to write on the network and discover other clients and servers
            UdpClient localListingsClient = new UdpClient(new IPEndPoint(ipAddress, 19375));
            localListingsClient.EnableBroadcast = true;
            CloseLocalListingsClient();

            IPEndPoint groupEp = default(IPEndPoint);
            string endpoint = string.Empty;

            localListingsClient.Send(new byte[] { BROADCAST_LISTING_REQUEST_1, BROADCAST_LISTING_REQUEST_2, BROADCAST_LISTING_REQUEST_3 }, 3,
                new IPEndPoint(IPAddress.Parse("255.255.255.255"), portNumber));

            try
            {
                byte[] data = localListingsClient.Receive(ref groupEp);

                if (data.Length != 1)
                    continue;

                string[] parts = endpoint.Split('+');
                string address = parts[0];
                ushort port = ushort.Parse(parts[1]);
                Debug.Log(address + "found");
                /*if (data[0] == SERVER_BROADCAST_CODE)
                {
                    var ep = new BroadcastEndpoints(address, port, true);
                    LocalEndpoints.Add(ep);

                }
                else if (data[0] == CLIENT_BROADCAST_CODE)
                    LocalEndpoints.Add(new BroadcastEndpoints(address, port, false));*/

            }
            catch
            { }
        }
    }

    public struct BroadcastEndpoints
    {
        public string Address { get; private set; }
        public ushort Port { get; private set; }
        public bool IsServer { get; private set; }

        public BroadcastEndpoints(string address, ushort port, bool isServer) : this()
        {
            this.Address = address;
            this.Port = port;
            this.IsServer = isServer;
        }
    }

    public static List<BroadcastEndpoints> LocalEndpoints { get; private set; }

    public void stop()
    {
        probe.Stop();
    }

    //public GameObject deviceItemUI, deviceItemsContainer;

    int devicesFound = 0;

    public List<string> ips;

    public GameObject searchText, hostButton;

    // Source: https://www.tutorialsrack.com/articles/409/how-to-check-if-ipv4-ip-address-is-local-or-not-in-csharp
    public static bool IsLocalIpAddress(string host)
    {
        try
        {
            // get host IP addresses
            IPAddress[] hostIPs = Dns.GetHostAddresses(host);
            // get local IP addresses
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

            // test if any host IP equals to any local IP or to localhost
            foreach (IPAddress hostIP in hostIPs)
            {
                // is localhost
                if (IPAddress.IsLoopback(hostIP)) return true;
                // is local address
                foreach (IPAddress localIP in localIPs)
                {
                    if (hostIP.Equals(localIP)) return true;
                }
            }
        }
        catch { }
        return false;
    }

    private void Update()
    {
        if (probe.isRunning)
        {
            if (Probe.deviceIps.Count != devicesFound)
            {
                devicesFound = Probe.deviceIps.Count;
                foreach (var item in Probe.deviceIps)
                {
                    if (ips.IndexOf(item) == -1)
                    {
                        ips.Add(item + ":" + IsLocalIpAddress(item).ToString());
                        Debug.Log("Found" + item);
                        Debug.Log("Is local" + IsLocalIpAddress(item));
                        GameObject.Find("DevicesFound").GetComponent<Text>().text = "Found: " + ips.Count.ToString();
                    }
                }
                /*devicesFound = Probe.deviceIps.Count;
                string text = ""; 
                foreach (Transform item in deviceItemsContainer.transform)
                {
                    Destroy(item.gameObject);
                }
                foreach (var item in Probe.deviceIps)
                {
                    GameObject newDevice = Instantiate(deviceItemUI);
                    newDevice.transform.SetParent(deviceItemsContainer.transform, false);
                    newDevice.transform.GetChild(0).GetComponent<Text>().text = item;
                    newDevice.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(delegate{connectToDevice(item);});
                }

                GameObject.Find("IPS").GetComponent<Text>().text = text;*/
            }
        }
    }

    void OnDisable()
    {
        probe.Stop();
    }
}
