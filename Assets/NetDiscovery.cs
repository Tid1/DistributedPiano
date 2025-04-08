// see blog post here: https://shoreparty.org/reverse-engineering-the-unity-network-discovery-protocol-9cd01280ed08

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class NetDiscovery : MonoBehaviour
{

    private static byte[] GetBytesForInt(int i) => BitConverter.GetBytes(i).Reverse().ToArray();

    private static byte[] CreateMessage()
    {
        var l = new List<byte>();

        l.AddRange(new byte[] { 0x00, 0x00, 0x09 }); // prefix

        var rand = new byte[2];
        new System.Random().NextBytes(rand);
        l.AddRange(rand); // random value per started session

        l.AddRange(GetBytesForInt(36001)); // KEY

        l.AddRange(Enumerable.Repeat(0, 4 * 8).Select(x => (byte)x)); // padding

        l.AddRange(GetBytesForInt(1)); // VER

        l.AddRange(GetBytesForInt(1)); // SUBVER

        l.AddRange(Encoding.ASCII.GetBytes("test data please ignore").Select(x => new byte[] { x, 0x00 }).SelectMany(y => y)); // DATA

        return l.ToArray();
    }

    void Start()
    {
        StartCoroutine("startAll");
    }
    public IEnumerator startAll()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.GetIPProperties().GatewayAddresses.Any()).ToList();
        var dict = interfaces.ToDictionary(x => x, y => y.GetIPProperties().GatewayAddresses[0].Address);

        var msg = CreateMessage();

        while (true)
        {
            yield return new WaitForSeconds(2);
            foreach (var id in dict)
            {
                if (!id.Value.ToString().Contains("127"))
                {

                    Debug.Log("Sending broadcast for interface" + id.Key.Name + id.Value);

                    var uc = new UdpClient(64764);
                    uc.EnableBroadcast = true; 
                    var broadcastIp = new IPAddress(id.Value.GetAddressBytes().Take(3).Concat(new[] { (byte)0xff }).ToArray());
                    uc.Send(msg, msg.Length, broadcastIp.ToString(), 19375);
                    uc.Close();
                }
            }
        }
    }
}