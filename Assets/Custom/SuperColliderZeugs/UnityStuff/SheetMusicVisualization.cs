using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using InternetTime;
using InternetTime.Custom.SuperColliderZeugs;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Note = Melanchall.DryWetMidi.Interaction.Note;


public class SheetMusicVisualization : MonoBehaviour {
    public event Action OnPieceOver;
    
    [SerializeField] private GameObject note;
    [SerializeField] private GameObject trebleNotesHolder;
    [SerializeField] private GameObject trebleBackgroundHolder;
    [SerializeField] private GameObject bassNotesHolder;
    [SerializeField] private GameObject bassBackgroundHolder;
    [SerializeField] private GameObject playLine;
    [SerializeField] private GameObject noteBackground;
    [SerializeField] private int maxTimeOfNote = 10;
    [SerializeField] private int delayAfterPieceEndInSecs = 3;


    //  [SerializeField] private Color flatColor;

    private const int C4 = 60;

    private const int WhiteKeysPerOctave = 7;
    private const int TotalKeysPerOctave = 12;
    private const int NumSpacesBetweenLines = 4;
    private const int TrebleOctaveMiddleNote = 4;
    private const int BassOctaveMiddleNote = 3;
    private const float SpawnOffsetInSecs = 1.5f;
    
    private TempoMap tempoMap;
    private List<Note> midiNotes;
    private List<(SimpleNote, RectTransform)> notesToPlay;
    private List<SimpleNote> parsedNotes;
    private MidiFile midiFile;
    private float backgroundHolderHeight;
    private float spacingBetweenLines;
    private float startTime;
    private float screenWidth;
    private float playLineXPos;
    private List<KeyObj> keyEventList;
    private List<int> keysToRemove;
    private SimpleNote lastNoteInPiece;
    
    [SerializeField]private bool hasStarted;

    private int currentNoteToProcess = 0;

    private long timeOfLastTimeSigChange;


    public void Start() {
        this.keysToRemove = new List<int>();
        this.keyEventList = new List<KeyObj>();
        this.parsedNotes = new List<SimpleNote>();
        this.notesToPlay = new List<(SimpleNote, RectTransform)>();
        this.playLineXPos = playLine.GetComponent<RectTransform>().anchoredPosition.x;
        this.backgroundHolderHeight = trebleBackgroundHolder.GetComponent<RectTransform>().rect.height;
        this.spacingBetweenLines = backgroundHolderHeight / NumSpacesBetweenLines;
        screenWidth = noteBackground.GetComponent<RectTransform>().rect.width;

        SimpleNote noteToPlace = new SimpleNote();
        noteToPlace.noteNumber = 57; //C4;
        noteToPlace.duration = 2;
        noteToPlace.startTime = 5;
        parsedNotes.Add(noteToPlace);
    }

    private void Update() {
        if (!hasStarted) return;
        SpawnNote();

        foreach (var (simpleNote, gameObj) in notesToPlay) {
            HandleGlidingMovement(simpleNote.startTime, gameObj);
        }

        foreach (var keyObj in keyEventList) {
            if (keyObj.shouldScale) ScalePressedNotes(keyObj.startTime, keyObj.rectTransform);
            HandleGlidingMovement(keyObj.startTime, keyObj.rectTransform);
        }
        FilterNotes();
        FilterOwnNotes();
        CheckPieceOver();
    }


    public void PrepareVisualization(List<SimpleNote> simpleNotes) {
        this.parsedNotes = new List<SimpleNote>(simpleNotes);
        parsedNotes.Sort((a, b) => a.startTime.CompareTo(b.startTime));
        this.lastNoteInPiece = parsedNotes[^1];
    }

    public void StartVisualization() {
        this.hasStarted = true;
        startTime = Time.time;
        Debug.Log("Visualizer: has started");
    }

    private void CheckPieceOver() {
        if (Time.time - startTime >= lastNoteInPiece.startTime + lastNoteInPiece.duration + delayAfterPieceEndInSecs) {
            hasStarted = false;
            OnPieceOver?.Invoke();
        }
    }

    private bool IsTrebleClef(int noteNumber) {
        return noteNumber >= C4;
    }

    private void PlaceNote(SimpleNote noteToPlace) {
        RectTransform newNoteRectTransform = CreateNote(noteToPlace.noteNumber, out Image noteImage);
        
        float noteImageLength = (screenWidth * noteToPlace.duration) / maxTimeOfNote;
        Vector2 noteSizeDelta = newNoteRectTransform.sizeDelta;
        noteSizeDelta.x = noteImageLength;
        newNoteRectTransform.sizeDelta = noteSizeDelta;
        if (IsSharp(noteToPlace.noteNumber)) noteImage.color = Color.blue;
        notesToPlay.Add((noteToPlace, newNoteRectTransform));
        
    }

    private RectTransform CreateNote(int noteNumber, out Image noteImage) {
        int actualPosition = noteNumber - 24;
        int octave =
            (actualPosition / TotalKeysPerOctave) + 1; //TODO gucken ob die +1 consistent ist (scheint grade so)
        bool isTrebleClef = IsTrebleClef(noteNumber);
        //NoteName noteName = noteToPlace.noteName + 3;
        float baseDistance = CalculateDistance(noteNumber, isTrebleClef);

        int clefOctave = isTrebleClef ? TrebleOctaveMiddleNote : BassOctaveMiddleNote;
        int distanceToBassOctave = octave - clefOctave;
        if (!isTrebleClef) distanceToBassOctave += 1;

        float noteHeightPos = baseDistance + (distanceToBassOctave * WhiteKeysPerOctave * spacingBetweenLines / 2);
        Transform noteHolder = isTrebleClef ? trebleNotesHolder.transform : bassNotesHolder.transform;
        GameObject newNote = Instantiate(note, noteHolder);

        RectTransform newNoteRectTransform = newNote.GetComponent<RectTransform>();

        newNoteRectTransform.anchorMin = new Vector2(0f, 0.5f);
        newNoteRectTransform.anchorMax = new Vector2(0f, 0.5f);
        newNoteRectTransform.pivot = new Vector2(1f, 0.5f);

        newNoteRectTransform.anchoredPosition3D = new Vector3(
            -10, //SpawnPosOffset
            noteHeightPos,
            0
        );

        newNoteRectTransform.sizeDelta = new Vector2(
            0,
            spacingBetweenLines * 0.3f
        );
        noteImage = newNote.GetComponent<Image>();

        return newNoteRectTransform;
    }

    private bool IsSharp(int noteNumber) {
        int noteNumberToName = noteNumber % 12;
        return (noteNumberToName == (int) NoteName.CSharp ||
                noteNumberToName == (int) NoteName.DSharp ||
                noteNumberToName == (int) NoteName.FSharp ||
                noteNumberToName == (int) NoteName.GSharp ||
                noteNumberToName == (int) NoteName.ASharp);
    }

    private void HandleGlidingMovement(float noteStartTime, RectTransform noteToMove) {
        float newXPos = (Time.time - startTime - noteStartTime) * (screenWidth / maxTimeOfNote) +
                        playLineXPos;
        Vector3 transformVector = noteToMove.anchoredPosition;
        transformVector.x = newXPos;
        noteToMove.anchoredPosition = transformVector;
        /*
        /*
        Debug.Log("NoteStartTime:  " + noteStartTime + ", PlayLineLocalX: " + playLineXPos
         + ", Screen Width: " + screenWidth + ", TimeMinusStart: " + (Time.time - startTime));*/
    }

    public void OnKeyOn(int key) {
        if (!hasStarted) return;
        float pressTimeForKey = Time.time - startTime;
        RectTransform newNoteRectTransform = CreateNote(key, out Image noteImage);
        if (IsSharp(key)) {
            noteImage.color = Color.red;
        } else {
            noteImage.color = Color.yellow;
        }

        keyEventList.Add(new KeyObj
            {key = key, startTime = pressTimeForKey, rectTransform = newNoteRectTransform, shouldScale = true});
        Debug.Log("KeyOn received: " + key);
    }

    public void OnKeyOff(int key) {
        if (!hasStarted) return;
        KeyObj itemToRemove = keyEventList.Find(obj => obj.key == key && obj.shouldScale);
        itemToRemove.shouldScale = false;
        Debug.Log("KeyOff received: " + key);
    }

    private void ScalePressedNotes(float noteStartTime, RectTransform noteRectTransform) {
        float noteLength = (Time.time - startTime - noteStartTime) * screenWidth / maxTimeOfNote;
        Vector2 noteSizeDelta = noteRectTransform.sizeDelta;
        noteSizeDelta.x = noteLength;
        noteRectTransform.sizeDelta = noteSizeDelta;
    }

    private void FilterNotes() {
        for (int i = notesToPlay.Count - 1; i >= 0; i--) {
            (SimpleNote _, RectTransform noteRectTransform) = notesToPlay[i];
            if (noteRectTransform.anchoredPosition.x > screenWidth + noteRectTransform.rect.width) {
                Destroy(noteRectTransform.gameObject);
                notesToPlay.RemoveAt(i);
            }
        }
    }

    private void FilterOwnNotes() {
        for (int i = keyEventList.Count - 1; i >= 0; i--) {
            KeyObj keyObj = keyEventList[i];
            if (keyObj.rectTransform.anchoredPosition.x > screenWidth + keyObj.rectTransform.rect.width) {
                Destroy(keyObj.rectTransform.gameObject);
                keyEventList.RemoveAt(i);
            }
        }
    }

    private void SpawnNote() {
        while (currentNoteToProcess < parsedNotes.Count) {
            SimpleNote noteAtIdx = parsedNotes[currentNoteToProcess];
            float spawnTime = noteAtIdx.startTime - (maxTimeOfNote * playLineXPos / screenWidth);
            if (Time.time - startTime >= spawnTime - SpawnOffsetInSecs) {
                PlaceNote(noteAtIdx);
                currentNoteToProcess++;
            } else {
                break;
            }
        }
    }

    private float CalculateDistance(int noteNumber, bool isTrebleClef) {
        float noteSpacing = spacingBetweenLines / 2;
        float distance = spacingBetweenLines / 2;
        switch (noteNumber % TotalKeysPerOctave) {
            case (int) NoteName.B:
                distance = 0;
                break;
            case (int) NoteName.A:
            case (int) NoteName.ASharp:
                distance *= -1;
                break;
            case (int) NoteName.G:
            case (int) NoteName.GSharp:
                distance *= -2;
                break;
            case (int) NoteName.F:
            case (int) NoteName.FSharp:
                distance *= -3;
                break;
            case (int) NoteName.E:
                distance *= -4;
                break;
            case (int) NoteName.D:
            case (int) NoteName.DSharp:
                distance *= -5;
                break;
            case (int) NoteName.C:
            case (int) NoteName.CSharp:
                distance *= -6;
                break;
        }

        if (!isTrebleClef) {
            distance += -2 * noteSpacing;
        }

        return distance;
    }

    private class KeyObj {
        public int key;
        public float startTime;
        public RectTransform rectTransform;
        public bool shouldScale;
    }
}
