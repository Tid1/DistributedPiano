namespace InternetTime.Custom.SuperColliderZeugs {
    using System.Collections.Generic;
    using System.Diagnostics;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    public delegate void KeyOnHandler(int key, float velocity, float startTime);
    public delegate void KeyOffHandler(int key, float endTime, long elapsedMs, long elapsedTicks);
    
    public class TimeMeasurement {
        public event KeyOnHandler OnKeyOn;
        public event KeyOffHandler OnKeyOff;
        
        private Dictionary<int, Stopwatch> noteStats = new Dictionary<int, Stopwatch>(); //Key: Played note; Value: Stopwatched for elapsed time
        private const int MAX_MIDI = 127;
        
        
        public TimeMeasurement() {
            SetupDictionary();
        }

        private void SetupDictionary() {
            for (int i = 0; i <= MAX_MIDI; i++) {
                noteStats[i] = new Stopwatch();
            }
        }
        
        public void KeyOn(int key, float velocity = 1f) {
            noteStats[key].Start();
            float startTime = Time.time;
            Debug.Log("Key "+ key + " pressed at: " + startTime);
            OnKeyOn?.Invoke(key, velocity, startTime);
        }

        public void KeyOff(int key) {
            noteStats[key].Stop();
            float endTime = Time.time;
            long elapsedMs = noteStats[key].ElapsedMilliseconds;
            long elapsedTicks = noteStats[key].ElapsedTicks;
            Debug.Log("Key " + key + " released at: " + endTime);
            Debug.Log("Key " + key  +" elapsed time: " + elapsedMs + "ms");
            Debug.Log("Key " + key + " elapsed ticks: " + elapsedMs);
            noteStats[key].Reset();
            OnKeyOff?.Invoke(key, endTime, elapsedMs, elapsedTicks);
        }
        
    }
}
