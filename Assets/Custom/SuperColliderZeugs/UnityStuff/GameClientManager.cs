namespace InternetTime.Custom.SuperColliderZeugs {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Melanchall.DryWetMidi.Core;
    using Melanchall.DryWetMidi.Interaction;
    using OSCData;
    using TMPro;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    public class GameClientManager : MonoBehaviour {
        private GameNetwork network;
        private TimeMeasurement measurement;

        [SerializeField] private Keyboard keyboard;
        [SerializeField] private GlobalSettings settings;
        //[SerializeField] private NoteVisualizer visualizer;
        [SerializeField] private SheetMusicVisualization visualizer;
        [SerializeField] private Scoring scoring;
        
        [SerializeField] private Text waitingText;
        [SerializeField] private TMP_InputField startOctaveInput;
        [SerializeField] private TMP_InputField numOctavesInput;
        [SerializeField] private Button configButton;
        [SerializeField] private Text scoreText;
        [SerializeField] private TMP_Text rttText;

        private string midiFileName;
        private MidiFile midiFile;
        private List<int> keysPressed = new List<int>();

        private float timePassed;
        private bool startReceived;
        private int timeframe = 1;
        private float scoredPoints;
        private bool receivedMidi;

        private void Start() {
            measurement = new TimeMeasurement();
            this.network = NetworkRegistry.Network;
            network.OnStartMusic += ReceiveStartMusic;
            network.OnReceiveOctaveConfig += ReceiveOctaveConfig;
            network.OnReceiveMidi += ReceiveMidiFile;
            network.OnRTTReceived += ReceiveRTT;

            measurement.OnKeyOn += (key, velocity, time) => network.SendKeyOnDebug(key, velocity, time);
            measurement.OnKeyOff += (key, time, ms, ticks) => network.SendKeyOffDebug(key, time, ms, ticks);
            measurement.OnKeyOff += scoring.ReceiveKey;

            visualizer.OnPieceOver += scoring.StopMusic;
            scoring.OnPieceEnd += LoadGameOverScene;
            configButton.onClick.AddListener(ChangeConfig);
            
            receivedMidi = false;
            waitingText.enabled = true;
            scoreText.enabled = false;
            startOctaveInput.enabled = true;
            numOctavesInput.enabled = true;
            configButton.enabled = true;
        }

        /*  private void FixedUpdate() {
              if (startReceived) {
                  timePassed += Time.deltaTime;
                  Debug.Log("Time passed: "+ timePassed);
                  CalculateScore();
              }
          }*/

        private void Update() {
            if (startReceived) {
                timePassed += Time.deltaTime;
                //Debug.Log("Time passed: " + timePassed);
                //CalculateScore();
            }
        }

        private void ReceiveStartMusic() {
            if(startReceived) return;
            UnityMainThreadDispatcher.Instance()
                .Enqueue(() => Invoke("StartVisualization", 1f /* -rtt hinzufügen */ / 2f / 1000f));
            startReceived = true;
            UnityMainThreadDispatcher.Instance().Enqueue(HideUI);
            //keyboard.OnKeyOn += KeyOn;
            keyboard.OnKeyOnVelocity += KeyOn;
            keyboard.OnKeyOff += KeyOff;
            keyboard.OnKeyOn += visualizer.OnKeyOn;
            keyboard.OnKeyOff += visualizer.OnKeyOff;
            if(receivedMidi) scoring.StartMusic();
        }

        private void HideUI() {
            waitingText.gameObject.SetActive(false);
            startOctaveInput.gameObject.SetActive(false);
            numOctavesInput.gameObject.SetActive(false);
            configButton.gameObject.SetActive(false);
            scoreText.gameObject.SetActive(true);
            scoreText.enabled = true;
        }

        private void StartVisualization() {
            visualizer.StartVisualization();
        }

        private void ReceiveOctaveConfig(int numOctaves, int startOctave) {
            UnityMainThreadDispatcher.Instance().Enqueue(() => UpdateKeyboardInterface(numOctaves, startOctave));
        }

        private void ReceiveMidiFile(string fileName, byte[] data) {
            List<SimpleNote> parsedNotes = new();
            midiFileName = fileName;
            MemoryStream midiStream = new MemoryStream(data);
            midiFile = MidiFile.Read(midiStream);
            TempoMap tempoMap = midiFile.GetTempoMap();
            List<Note> midiNotes = midiFile.GetNotes().ToList();
            foreach (Note note in midiNotes) {
                SimpleNote simpleNote = new SimpleNote();
                simpleNote.startTime = note.TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000000f;
                simpleNote.duration = note.LengthAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000000f;
                simpleNote.noteNumber = note.NoteNumber;
                simpleNote.noteName = note.NoteName;
                parsedNotes.Add(simpleNote);
            }
            visualizer.PrepareVisualization(parsedNotes);
            scoring.SetParsedNotes(parsedNotes);
            receivedMidi = true;
            Debug.Log("Midi Notes length: " + midiNotes.Count);
        }

        private void UpdateKeyboardInterface(int numOctaves, int startOctave) {
            settings.startOctave = startOctave;
            settings.numberOfOctaves = numOctaves;
            keyboard.CreateKeyboardUI();
        }

        private void OnDestroy() {
            network.Stop();
        }

        private void KeyOn(int key, float velocity = 1f) {
            if (keysPressed.IndexOf(key) == -1) {
                keysPressed.Add(key);
                measurement.KeyOn(key, velocity);
                Debug.Log("Added Key: " + key);
            }

            //network.SendKeyOn(key);
        }
        

        private void KeyOff(int key) {
            keysPressed.Remove(key);
            measurement.KeyOff(key);
            //network.SendKeyOff(key);
        }

        private void ReceiveRTT(long rtt) {
            UnityMainThreadDispatcher.Instance().Enqueue(
                () => rttText.text = $"RTT: {rtt / TimeSpan.TicksPerMillisecond}ms"
            );
        }

        private void ChangeConfig() {
            try {
                int startOctave = Int32.Parse(startOctaveInput.text);
                int numOctaves = Int32.Parse(numOctavesInput.text);
                settings.startOctave = startOctave;
                settings.numberOfOctaves = numOctaves;

                keyboard.CreateKeyboardUI();
            } catch (FormatException) {
                Debug.Log("Wrong Input Format!");
            }
        }

        private void LoadGameOverScene(float achievedScore, float maximumScore) {
            ScoringRegistry.achievedScore = achievedScore;
            ScoringRegistry.maximumScore = maximumScore;
            SceneManager.LoadScene("GameOverScene");
        }

        /*  private GameNetwork network;
          private List<string> availableHosts;
  
          public void Start() {
              network = new GameNetwork();
              availableHosts = new();
          }
  
          private void ReceiveHostInformation(List<string> lobbies) {
              this.availableHosts = new List<string>(lobbies);
          }
          
          private void RequestJoin(string lobbyName) {
              network.SendJoinReq(lobbyName);
          }*/
    }
}
