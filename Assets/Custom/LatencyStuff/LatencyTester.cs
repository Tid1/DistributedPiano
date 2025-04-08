using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LatencyTester : MonoBehaviour {
   [SerializeField] private int startTimeInSeconds;
   [SerializeField] private Button startButton;
   [SerializeField] private Button stopButton;
   [SerializeField] private TMP_Text timerText;
   [SerializeField] private GameObject background;
   [SerializeField] private Button toggleBackgroundButton;
   
   private Stopwatch stopwatch;
   private bool changeTimerText;
   
   private void Start() {
      this.changeTimerText = false;
      this.stopwatch = new Stopwatch();
      this.background.SetActive(false);
      this.startButton.onClick.AddListener(StartTimer);
      this.stopButton.onClick.AddListener(StopTimer);
      this.toggleBackgroundButton.onClick.AddListener(ToggleBackground);
   }

   private void Update() {
      if (changeTimerText) {
         timerText.text = "Timer: " + stopwatch.ElapsedMilliseconds;
      } 
   }


   private void StartTimer() {
      changeTimerText = true;
      stopwatch.Reset();
      this.stopwatch.Start();
   }

   private void StopTimer() {
      this.changeTimerText = false;
      stopwatch.Stop();
      long elapsedTimeInMs = stopwatch.ElapsedMilliseconds - (startTimeInSeconds * 1000);
      timerText.text = "Elapsed Time in MS: " + elapsedTimeInMs + "ms";
   }

   private void ToggleBackground() {
      bool isActive = this.background.activeSelf;
      this.background.SetActive(!isActive);
   }
   
}
