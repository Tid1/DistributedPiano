using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OscJack;
using Newtonsoft.Json;
using System;
using AudioHelm;
using UnityEngine.UI;

public class OSCNetwork : MonoBehaviour
{
    /*
Created by Marvin Krueger
This file was created by us. Intially used for another project and adjust for the purpose of this project
 */
    OscServer server;

    string hostIp = "";
    OscClient nodesClient;

    public HelmController controller;
    public bool useHelm = true;
    public UnitySoundGen soundGen;

    bool transmittedHost = false;

    // Start is called before the first frame update
    void Start()
    {
        localIpAddress = GetLocalIPAddress();
        server = new OscServer(9000); // Port number

        server.MessageDispatcher.AddCallback(
            "/test", // OSC address
            (string address, OscDataHandle data) =>
            {
                handleReceivedData(data);
            }
        );

        server.MessageDispatcher.AddCallback(
            "/transmitHost", // OSC address
            (string address, OscDataHandle data) =>
            {
                string ip = data.GetElementAsString(0);
                Debug.Log("Host is" + ip);
                hostIp = ip;
                transmittedHost = true;
                bool alreadyHasClient = false;
                UnityMainThreadDispatcher.Instance().Enqueue(getRTT());
                if (connectedHosts.ContainsKey(ip))
                {
                    Debug.Log("already connected");
                    alreadyHasClient = true;
                    nodesClient = connectedHosts[ip];
                }
                if (!alreadyHasClient)
                {
                    nodesClient = new OscClient(hostIp, 9000);
                }
            }
        );

        server.MessageDispatcher.AddCallback(
            "/start", // OSC address
            (string address, OscDataHandle data) =>
            {
                Debug.Log("Start package received");
                Debug.Log("Time as string" + data.GetElementAsString(0));
                startSignalReceived = true;
                hasStarted = false;
                try
                {
                    startTime = Int64.Parse(data.GetElementAsString(0).Split('.')[0]);
                }
                catch
                {
                    Debug.Log("Wrong format" + data.GetElementAsString(0));
                }
                UnityMainThreadDispatcher.Instance().Enqueue(startVisualization());
            }
        );

        server.MessageDispatcher.AddCallback(
            "/keyOn", // OSC address
            (string address, OscDataHandle data) =>
            {
                int key = data.GetElementAsInt(0);
                Debug.Log("Key on " + key);
                startKeyOnPerNote[key] = DateTime.Now;
                if (delayedKeyOffPerNote.ContainsKey(key))
                {
                    delayedKeyOffPerNote.Remove(key);
                    if (useHelm)
                    {
                        controller.NoteOff(key);
                    }
                    else
                    {
                        soundGen.onKeyOff(key);
                    }
                }
                if (useHelm)
                {
                    controller.NoteOn(key);
                }
                else
                {
                    soundGen.OnKey(key);
                }
            }
        );

        server.MessageDispatcher.AddCallback(
            "/keyOff", // OSC address
            (string address, OscDataHandle data) =>
            {
                int key = data.GetElementAsInt(0);
                Debug.Log("Key off " + key);
                if (DateTime.Now.Subtract(startKeyOnPerNote[key]).Seconds < 0.1f)
                {
                    Debug.Log("Delayed " + key);
                    delayedKeyOffPerNote[key] = DateTime.Now;
                }
                else
                {
                    if (useHelm)
                    {
                        controller.NoteOff(key);
                    }
                    else
                    {
                        soundGen.onKeyOff(key);
                    }
                }
            }
        );

        server.MessageDispatcher.AddCallback(
            "/point", // OSC address
            (string address, OscDataHandle data) =>
            {
                float points = float.Parse(data.GetElementAsString(0).Split(':')[0]);
                string senderIpAdress = data.GetElementAsString(0).Split(':')[1];
                scorePerNode[senderIpAdress] = points;
                Debug.Log("point " + points);
            }
        );

        server.MessageDispatcher.AddCallback(
            "/configOctaves", // OSC address
            (string address, OscDataHandle data) =>
            {
                octaves = int.Parse(data.GetElementAsString(0));
                startOctave = int.Parse(data.GetElementAsString(1));
                Debug.Log("Start octave" + startOctave);
                keyboardConfigChanged = true;
            }
        );

        //createClientHost("127.0.0.1");
    }

    float rtt;

    // https://docs.microsoft.com/en-us/dotnet/api/system.net.networkinformation.pingreply.roundtriptime?view=net-6.0
    public IEnumerator getRTT()
    {
        for (int i = 0; i < 10; i++)
        {
            var ping = new Ping(hostIp);

            yield return new WaitForSeconds(0.1f);
            while (!ping.isDone)
                yield return null;

            Debug.Log(ping.time);
            rtt += (ping.time);
        }
        rtt /= 10f;
        Debug.Log("AVG" + rtt);
        yield return null;
    }

    public IEnumerator startVisualization()
    {
        Debug.Log("Invoke start");
        if (TimeMeasurementType.instance.type == TimeMeasurementType.TimeMeasurement.StartSignal)
        {
            // TODO: Add rtt here

            Invoke("startVisualizationNow", 1f - rtt / 2f / 1000f);
        }
        else if (TimeMeasurementType.instance.type == TimeMeasurementType.TimeMeasurement.Christian)
        {
            Invoke("startVisualizationNow", startTime - TimeFetcher.instance.timestampChristian);
        }
        yield return null;
    }

    public void startVisualizationNow()
    {
        // Start signal method
        hasStarted = true;
        Camera.main.GetComponent<NoteVisualizer>().StartPlaying();
        Debug.Log("Start now");
    }

    public Dictionary<int, DateTime> startKeyOnPerNote = new Dictionary<int, DateTime>();
    public Dictionary<int, DateTime> delayedKeyOffPerNote = new Dictionary<int, DateTime>();
    public List<int> delayedKeysOff = new List<int>();

    public Dictionary<string, float> scorePerNode = new Dictionary<string, float>();

    bool keyboardConfigChanged;
    int octaves, startOctave;

    public void OnKey(int key)
    {
        nodesClient.Send(
            "/keyOn", // OSC address
            key
        ); // Second element
    }

    public void OffKey(int key)
    {
        nodesClient.Send(
            "/keyOff", // OSC address
            key
        ); // Second element
    }

    public void setPoints(float score)
    {
        Debug.Log("Send points");
        nodesClient.Send(
            "/point", // OSC address
            score.ToString() + ":" + localIpAddress
        ); // Second element
    }

    Int64 startTime;

    Dictionary<int, string> receivedJSONNotes = new Dictionary<int, string>();
    int lastMessageIndex = -1;

    public void handleReceivedData(OscDataHandle data)
    {
        /*Debug.Log(string.Format("({0})",
                        data.GetElementAsString(0)));*/
        var pieces = data.GetElementAsString(0).Split(new[] { ':' }, 2);
        int index = int.Parse(pieces[0].ToString());
        string myString = pieces[1].ToString();
        receivedJSONNotes[index] = myString;
        if (myString.Substring(myString.ToCharArray().Length - 2) == "}]")
        {
            lastMessageIndex = index;
        }
        ;

        if (lastMessageIndex != -1 && receivedJSONNotes.Keys.Count == lastMessageIndex + 1)
        {
            Debug.Log("Complete!");
            mergeNotes();
        }
    }

    List<SimpleNote> notes = null;

    string localIpAddress;
    bool hasSetNotes,
        hasStarted = false;
    bool startSignalReceived = false;

    public void mergeNotes()
    {
        string notesString = "";
        for (int i = 0; i <= lastMessageIndex; i++)
        {
            notesString += receivedJSONNotes[i];
        }
        notes = JsonConvert.DeserializeObject<List<SimpleNote>>(notesString);
        Debug.Log("Notes received");
    }

    public Text scoreText;

    public void Update()
    {
        if (delayedKeyOffPerNote.Keys.Count > 0)
        {
            List<int> removedKeys = new List<int>();
            foreach (KeyValuePair<int, DateTime> item in delayedKeyOffPerNote)
            {
                if (DateTime.Now.Subtract(item.Value).Seconds > 0.15f)
                {
                    removedKeys.Add(item.Key);
                    if (useHelm)
                    {
                        controller.NoteOff(item.Key);
                    }
                    else
                    {
                        soundGen.onKeyOff(item.Key);
                    }
                }
            }

            foreach (var item in removedKeys)
            {
                delayedKeyOffPerNote.Remove(item);
            }
        }
        if (isHost)
        {
            float scoreSum = 0;

            foreach (var item in scorePerNode.Values)
            {
                scoreSum += item;
            }
            scoreText.text = scoreSum.ToString();
        }
        if (keyboardConfigChanged)
        {
            keyboardConfigChanged = false;
            GameObject.Find("Canvas").GetComponent<GlobalSettings>().startOctave = startOctave;
            GameObject.Find("Canvas").GetComponent<GlobalSettings>().numberOfOctaves = octaves;
            GameObject.Find("Canvas").GetComponent<Keyboard>().CreateKeyboardUI();
        }
        if (notes != null && !hasSetNotes)
        {
            hasSetNotes = true;
            Camera.main.GetComponent<NoteVisualizer>().notesReceived = notes;
        }

        if (!hasStarted && startSignalReceived)
        {
            hostOctaves.gameObject.SetActive(false);
            clientOctaves.gameObject.SetActive(false);
            startOctaveText.gameObject.SetActive(false);
            loadMidiButton.SetActive(false);
            foundText.SetActive(false);
            startBtn.SetActive(false);
            timeSyncText.SetActive(false);
        }

        /*if (!hasStarted && startSignalReceived && TimeMeasurementType.instance.type == TimeMeasurementType.TimeMeasurement.StartSignal)
        {
           
        }*/

        if (
            !hasStarted
            && startSignalReceived
            && TimeMeasurementType.instance.type == TimeMeasurementType.TimeMeasurement.Christian
            && (Int64)TimeFetcher.instance.timestampChristian >= startTime
        )
        {
            hasStarted = true;
            Debug.Log(startTime);
            Camera.main.GetComponent<NoteVisualizer>().StartPlaying();
            Debug.Log("Start now");
        }

        if (
            !hasStarted
            && startSignalReceived
            && TimeMeasurementType.instance.type == TimeMeasurementType.TimeMeasurement.SNTP
            && (Int64)TimeFetcher.instance.timestampNTP >= startTime
        )
        {
            hasStarted = true;
            Camera.main.GetComponent<NoteVisualizer>().StartPlaying();
            Debug.Log("Start now");
        }
    }

    void OnDisable()
    {
        server.Dispose();

        if (client != null)
        {
            // Terminate the client.
            client.Dispose();
        }
    }

    OscClient client;
    Dictionary<string, OscClient> connectedHosts = new Dictionary<string, OscClient>();

    public InputField hostOctaves,
        clientOctaves,
        startOctaveText;

    public GameObject loadMidiButton,
        foundText,
        startBtn,
        timeSyncText;

    public void sentInitialData(string hostIp, bool isHost)
    {
        // IP address, port number
        // TODO: We need to check if client is there two times :&
        //client = new OscClient(hostIp, 9000);
        //connectedHosts[hostIp] = client;

        string json = JsonConvert.SerializeObject(
            Camera.main.GetComponent<MIDIParser>().notes,
            Formatting.None
        );
        Debug.Log(json.ToCharArray().Length);
        for (int i = 0; i <= (int)json.ToCharArray().Length / 2048; i++)
        {
            if (i * 2048 + 2048 > json.ToCharArray().Length)
            {
                client.Send(
                    "/test", // OSC address
                    i.ToString() + ":" + json.Substring(i * 2048)
                ); // Second element
            }
            else
            {
                client.Send(
                    "/test", // OSC address
                    i.ToString() + ":" + json.Substring(i * 2048, 2048)
                ); // Second element
            }
        }

        client.Send(
            "/transmitHost", // OSC address
            hostIp
        ); // Second element

        if (isHost)
        {
            Debug.Log("Octaves" + hostOctaves.text.ToString());
            client.Send(
                "/configOctaves", // OSC address
                int.Parse(hostOctaves.text.ToString() != "" ? hostOctaves.text.ToString() : "3"),
                int.Parse(
                    startOctaveText.text.ToString() != "" ? startOctaveText.text.ToString() : "0"
                )
            ); // Second element
        }
        else
        {
            client.Send(
                "/configOctaves", // OSC address
                int.Parse(
                    clientOctaves.text.ToString() != "" ? clientOctaves.text.ToString() : "3"
                ),
                int.Parse(hostOctaves.text.ToString() != "" ? hostOctaves.text.ToString() : "3")
                    + int.Parse(
                        startOctaveText.text.ToString() != ""
                          ? startOctaveText.text.ToString()
                          : "0"
                    )
            ); // Second element
        }
    }

    public GameObject searchText,
        connectBtn;

    public static string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }

    bool isHost = false;

    public void clickConnectFromHost()
    {
        isHost = true;
        searchText.SetActive(false);
        connectBtn.SetActive(false);
        this.GetComponent<NetworkDetectionHost>().stop();
        string hostIp = "";
        // Create clients
        foreach (var item in this.GetComponent<NetworkDetectionHost>().ips)
        {
            string ip = item.Split(':')[0];
            string isLocal = item.Split(':')[1];
            if (isLocal == "True")
            {
                hostIp = ip;
            }
        }

        Debug.Log("Send notes and host ip to all clients");
        foreach (var item in this.GetComponent<NetworkDetectionHost>().ips)
        {
            Debug.Log("Send notes and host ip to" + item.Split(':')[0]);
            // Transmit data to all hosts
            client = new OscClient(item.Split(':')[0], 9000);
            connectedHosts[item.Split(':')[0]] = client;
            sentInitialData(hostIp, hostIp == item.Split(':')[0]);
        }
    }

    public void runMusic()
    {
        foreach (var item in connectedHosts.Values)
        {
            if (
                TimeMeasurementType.TimeMeasurement.StartSignal == TimeMeasurementType.instance.type
            )
            {
                // Date does not matter
                item.Send(
                    "/start",
                    (
                        DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds + 1000f
                    ).ToString()
                );
            }
            else if (
                TimeMeasurementType.TimeMeasurement.Christian == TimeMeasurementType.instance.type
            )
            {
                // Take time calculated with the christian algorithm and set starttime to t0+1s
                item.Send(
                    "/start",
                    (TimeFetcher.instance.timestampChristian + 1000.0).ToString("R")
                );
            }
            else if (TimeMeasurementType.TimeMeasurement.SNTP == TimeMeasurementType.instance.type)
            {
                item.Send("/start", (TimeFetcher.instance.timestampNTP + 1000.0).ToString("R"));
            }
        }
    }
}
