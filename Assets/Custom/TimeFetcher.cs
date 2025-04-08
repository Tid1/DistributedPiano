using UnityEngine;
using System; 
using System.Net; 
using System.Net.Sockets;
using InternetTime;

public class TimeFetcher : MonoBehaviour {
        /* 
    Created by Marvin Krueger
    This file was created by us. Intially used for another project and adjust for the purpose of this project
     */
    public long timestampNTP = -1;

    public static TimeFetcher instance;
    public long timestampChristian = -1;
    bool received = false; 
    SNTPClient client;
    private void Start() {
        TimeFetcher.instance = this;
        timestampChristian = (long)GetNetworkTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
        //client = new SNTPClient( "pool.ntp.org");
        //client.Connect(true);

       
    }
    int counter = 0; 

    bool invoked = false; 

    private void Update() {
        /*if(client.isReady && !invoked && TimeMeasurementType.instance.type == TimeMeasurementType.TimeMeasurement.SNTP){
            timestampNTP = (long)client.actualTime.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            client.isReady = false; 
            invoked = true; 
            Debug.Log("Invoke");
           Invoke("getTimeNTP", 1.5f);
        } else if(timestampNTP != -1){
            timestampNTP += (long)(Time.deltaTime * 1000f);
        }*/

        if(timestampChristian != -1){
            timestampChristian += (long)(Time.deltaTime * 1000f);
        }else if(!invoked){
            invoked = true;
           Invoke("getChristianTime", 1.5f);
        }
    }


    public void getTimeNTP(){
        client = new SNTPClient( "pool.ntp.org");
         client.Connect(true);
         invoked = false;
    }

     public void getChristianTime(){
        timestampChristian = (long)GetNetworkTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
         invoked = false;
    }
 

 private long ConvertToTimestamp(DateTime value)
{
    long epoch = (value.Ticks - 621355968000000000) / 10000000;
    return epoch;
}
 public DateTime GetNetworkTime()
{
  const string NtpServer = "pool.ntp.org";

  const int DaysTo1900 = 1900 * 365 + 95; // 95 = offset for leap-years etc.
  const long TicksPerSecond = 10000000L;
  const long TicksPerDay = 24 * 60 * 60 * TicksPerSecond;
  const long TicksTo1900 = DaysTo1900 * TicksPerDay;

  var ntpData = new byte[48];
  ntpData[0] = 0x1B; // LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

  var addresses = Dns.GetHostEntry(NtpServer).AddressList;
  var ipEndPoint = new IPEndPoint(addresses[0], 123);
  long pingDuration = System.Diagnostics.Stopwatch.GetTimestamp(); // temp access (JIT-Compiler need some time at first call)
  using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
  {
    socket.Connect(ipEndPoint);
    socket.ReceiveTimeout = 5000;
    socket.Send(ntpData);
    pingDuration = System.Diagnostics.Stopwatch.GetTimestamp(); // after Send-Method to reduce WinSocket API-Call time

    socket.Receive(ntpData);
    pingDuration = System.Diagnostics.Stopwatch.GetTimestamp() - pingDuration;
    socket.Close();
  }

  long pingTicks = pingDuration * TicksPerSecond / System.Diagnostics.Stopwatch.Frequency;

  // optional: display response-time
  // Console.WriteLine("{0:N2} ms", new TimeSpan(pingTicks).TotalMilliseconds);

  long intPart = (long)ntpData[40] << 24 | (long)ntpData[41] << 16 | (long)ntpData[42] << 8 | ntpData[43];
  long fractPart = (long)ntpData[44] << 24 | (long)ntpData[45] << 16 | (long)ntpData[46] << 8 | ntpData[47];
  long netTicks = intPart * TicksPerSecond + (fractPart * TicksPerSecond >> 32);

  var networkDateTime = new DateTime(TicksTo1900 + netTicks + pingTicks / 2);
  received = true;

  return networkDateTime; // without ToLocalTime() = faster
}
}