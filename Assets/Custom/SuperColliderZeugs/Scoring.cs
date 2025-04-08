namespace InternetTime.Custom.SuperColliderZeugs {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using Debug = UnityEngine.Debug;

    public delegate void OnPieceEndHandler(float achievedScore, float maximumScore);
    
    public class Scoring : MonoBehaviour {
        public event OnPieceEndHandler OnPieceEnd;
        [SerializeField] private float leniency;
        [SerializeField] private Text scoreText;

        [SerializeField] private float baseScore = 100f;

        //Used to Add more Score for longer Notes
        [SerializeField] private float multiplier = 0.2f;
        private float totalScore;
        private float maximumScore;
        private bool started;
        private List<SimpleNote> parsedNotes;
        private List<SimpleNote> notesToRemove;
        private Stopwatch timer = new Stopwatch();

        public void Update() {
            if(notesToRemove == null) return;
            foreach (SimpleNote note in notesToRemove) {
                parsedNotes.Remove(note);
            }
            notesToRemove.Clear();
        }

        public void SetParsedNotes(List<SimpleNote> simpleNotes) {
            totalScore = 0;
            maximumScore = 0;
            this.parsedNotes = new List<SimpleNote>(simpleNotes);
            this.notesToRemove = new List<SimpleNote>();
            parsedNotes.Sort((a, b) => a.startTime.CompareTo(b.startTime));
            CalculateMaximumScore();
        }

        public void StartMusic() {
            timer.Start();
            this.started = true;
        }

        //TODO Implement System for stopping Music
        public void StopMusic() {
            timer.Stop();
            this.started = false;
            OnPieceEnd?.Invoke(totalScore, maximumScore);
        }

        public void ReceiveKey(int key, float endTime, long noteDuration, long elapsedTicks) {
            if (!started) return;
            float noteDurationInSecs = noteDuration / 1000f;
            float noteEndTime = timer.ElapsedMilliseconds / 1000f;
            float noteStartTime = noteEndTime - noteDurationInSecs;

            SimpleNote noteInPiece = parsedNotes.FirstOrDefault(note => note.noteNumber == key &&
                                                                        note.startTime >= noteStartTime - leniency &&
                                                                        note.startTime + note.duration <=
                                                                        noteEndTime + leniency);

            Debug.Log("Note in piece: " + noteInPiece);
            if (noteInPiece == null) return;
            Debug.Log("Found noteInPiece: " + noteInPiece.noteNumber);
            Debug.Log("Scoring Key Start Time: " + noteStartTime);
            Debug.Log("Scoring Key End Time: " + noteEndTime);
            Debug.Log("Scoring Key Actual Start Time: " + noteInPiece.startTime);
            Debug.Log("Scoring Note in Piece Duration: " + noteInPiece.duration);
            Debug.Log("Scoring Note Duration In Secs: " + noteDurationInSecs);
            
            float prePercentageHit =  noteDurationInSecs / noteInPiece.duration;
            float actualPercentageHit = 1 - Math.Abs(1 - prePercentageHit); //1 == 100%
            float baseScoreForHit = baseScore * actualPercentageHit;
            float actualScoreForHit = baseScoreForHit + (multiplier * noteInPiece.duration);

            Debug.Log("Pre Percentage Hit: " + prePercentageHit + " Percentage Hit: " + actualPercentageHit + " Base Score: " + baseScoreForHit + " Actual Score: " +
                      actualScoreForHit);
            this.totalScore += actualScoreForHit;
            notesToRemove.Add(noteInPiece);
            scoreText.text = "" + totalScore;
        }

        private void CalculateMaximumScore() {
            foreach (SimpleNote note in parsedNotes) {
                maximumScore += baseScore + (multiplier * note.duration);
            }
        }
    }
}
