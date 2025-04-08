using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System;

namespace NetworkDeviceDiscovery
{

    public class Beacon : MonoBehaviour
    {

        public int Port = 15937;
        public Logger.LogLevel Loglevel = Logger.LogLevel.Info;
        public int BroadcastSignalFrequency = 1;

        bool kill = false;
        BeaconDevice currentBeaconDevice;
        byte[] currentBeaconDeviceJSONData;

        static Beacon currentBeacon;

        // Use this for initialization
        void Awake()
        {

            if (currentBeacon != null)
            {
                GameObject.Destroy(this.gameObject);
                return;
            }
            else
            {
                currentBeacon = this;
                DontDestroyOnLoad(gameObject);
            }

            currentBeaconDevice = new BeaconDevice(Device.CurrentDevice, Port);
            currentBeaconDeviceJSONData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(currentBeaconDevice));

        }

        void Start()
        {
            Thread t = new Thread(this.Loop);
            t.Start();
        }
        static string message;

        void OnDisable()
        {
            kill = true;
        }

        public string ip = "";

        void Loop()
        {

            Logger.LogInfo("Starting net discovery to send broadcast from port " + Port.ToString() + " to port  " + 19375.ToString(), Loglevel);

            var beaconUDP = new UdpClient(Port);
            beaconUDP.EnableBroadcast = true;

            try
            {

                while (!kill)
                {
                    //Logger.LogDebug ("Sending beacon signal",Loglevel);
                    // TODO: Was 8887
                    var BroadcastEntpoint = new IPEndPoint(ip == "" ? IPAddress.Parse("255.255.255.255") : IPAddress.Parse(ip), 19375);
                    try
                    {
                        Logger.LogInfo("Sent broadcast signal to " + (ip == "" ? "255.255.255.255" : ip), Loglevel);

                        beaconUDP.Send(currentBeaconDeviceJSONData, currentBeaconDeviceJSONData.Length, BroadcastEntpoint);
                    }
                    catch (SocketException)
                    {
                        //perhaps the internet is unreachable, let's wait and try again
                    }

                    Thread.Sleep((int)((1.0f / (float)BroadcastSignalFrequency) * 1000.0f));
                }

                Logger.LogInfo("Stopping beacon", Loglevel);
            }
            catch (ThreadAbortException)
            {
                //Unity kills the thread when it needs to, no worries here
            }
            catch (Exception e)
            {
                Logger.LogError(e.GetType().ToString() + " " + e.Message, Loglevel);
            }
            finally
            {
                beaconUDP.Close();
            }
        }
    }

}