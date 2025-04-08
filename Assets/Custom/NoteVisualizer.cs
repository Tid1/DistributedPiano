using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Interaction;
using System.Linq;
#if UNITY_STANDALONE || UNITY_EDITOR
using SFB;
#endif
#if UNITY_IOS || UNITY_ANDROID
using NativeFilePickerNamespace;
#endif

public delegate void ScoreHandler(float scoredPoints);

public class NoteVisualizer : MonoBehaviour {
    public event ScoreHandler OnScoredPoints;
    /*
    Created by Marvin Krueger
    This file was created by us. Intially used for another project and adjust for the purpose of this project
     */

    public GameObject note;
    public List<SimpleNote> notesReceived;

    bool playing = false;
    float timePassed = 0f;

    private const int WhiteKeysPerOctave = 7;
    private const int TotalKeysPerOctave = 12;

    private void Start() {
        settings = GameObject.Find("Canvas").GetComponent<GlobalSettings>();
    }

    public void StartPlaying(List<SimpleNote> notes) {
        this.notesReceived = notes;
        StartPlaying();
    }

    public void StartPlaying() {
        Debug.Log("Playing!");
        noteImages = new List<GameObject>();
        actualNotes = new List<SimpleNote>();
        playing = true;
        timePassed = 0f;

        List<GameObject> itemsToRemove = new List<GameObject>();
        foreach (GameObject item in noteImages) {
            Destroy(item.gameObject);
            itemsToRemove.Add(item);
        }

        foreach (var item in itemsToRemove) {
            noteImages.Remove(item);
        }
    }

    // Update is called once per frame

    public GameObject noteBackground;
    public int lastDrawn = -1;

    public List<GameObject> noteImages;

    int timeframe = 1;

    float score = 0;

    private float startTime;
    private float lastTime;
    private bool isInitialized = false;

    void Update() {
        if (playing) {
            if (!isInitialized) {
                startTime = Time.time;
                lastTime = startTime;
                isInitialized = true;
            }

            float currentTime = Time.time;
            float deltaTime = currentTime - lastTime;
            timePassed = currentTime - startTime;
            lastTime = currentTime;

            List<SimpleNote> newNotes = notesReceived.FindAll(
                note => (int) (note.startTime) == (int) timePassed
            );

            float height = noteBackground.GetComponent<RectTransform>().rect.height;
            float width = noteBackground.GetComponent<RectTransform>().rect.width;

            // in 10 seconds we need to move down exactly by value height
            if (lastDrawn != (int) timePassed) {
                lastDrawn = (int) timePassed;
                drawRow(newNotes);
            }

            HandleFallingNotes(height, deltaTime);
           // HandleGlidingNotes(width, deltaTime);
            
            // Delete notes which have passed
            actualNotes.RemoveAll(
                note => (note.startTime) + note.duration + timeframe + 1 < timePassed
            );

            foreach (SimpleNote item in actualNotes) {
                if (
                    keysPressed.Contains(item.noteNumber)
                    && item.startTime + timeframe <= timePassed
                    && item.startTime + timeframe + item.duration >= timePassed
                ) {
                    // 100 points per second per exact played note
                    score += 100 * deltaTime;
                    OnScoredPoints?.Invoke(score);
                }
            }
        }
    }

    public void drawRow(IEnumerable<SimpleNote> notes) {
        foreach (SimpleNote item in notes) {
            actualNotes.Add(item); 
            CreateNoteVertically(item.noteNumber, item.startTime, item.duration, item);
           // CreateNoteHorizontally(item.noteNumber, item.startTime, item.duration);
        }
    }

    public void HandleFallingNotes(float height, float deltaTime) {
        List<GameObject> itemsToRemove = new List<GameObject>();
        foreach (GameObject item in noteImages) {
            item.GetComponent<RectTransform>().localPosition +=
                Vector3.down * height * deltaTime / timeframe;

            if (item.GetComponent<RectTransform>().localPosition.y < -height * 4) {
                Destroy(item.gameObject);
                itemsToRemove.Add(item);
            }
        }

        foreach (var item in itemsToRemove) {
            noteImages.Remove(item);
        }
    }

    public void HandleGlidingNotes(float width, float deltaTime) {
        List<GameObject> itemsToRemove = new List<GameObject>();
        foreach (GameObject item in noteImages) {
            item.GetComponent<RectTransform>().localPosition +=
                Vector3.right * width * deltaTime / timeframe;

            if (item.GetComponent<RectTransform>().localPosition.x < -width * 4) {
                Destroy(item.gameObject);
                itemsToRemove.Add(item);
            }
        }

        foreach (var item in itemsToRemove) {
            noteImages.Remove(item);
        }
    }

    public List<int> keysPressed = new List<int>();

    public void keyOn(int midiNote) {
        if (keysPressed.IndexOf(midiNote) == -1) {
            keysPressed.Add(midiNote);
        }
        /*foreach (SimpleNote item in actualNotes)
        {
            float actualTime = timePassed;

            if(item.startTime+timeframe <= actualTime &&item.startTime+timeframe+item.duration >= actualTime){
                // currently playing
                //Debug.Log("Playing" + item.noteNumber);
                if(midiNote == item.noteNumber){
                    GameObject.Find("Point").GetComponent<Animation>().Play();
                    network.addPoints(1);
                }
            }
        }

        // https://www.inspiredacoustics.com/en/MIDI_note_numbers_and_center_frequencies
        float freq = 440 * Mathf.Pow(2, ((float)midiNote-69f)/12f);
        Camera.main.GetComponent<UnitySoundGen>().OnKey(midiNote, freq);*/
    }

    public void keyOff(int midiNote) {
        keysPressed.Remove(midiNote);
        //Camera.main.GetComponent<UnitySoundGen>().onKeyOff(midiNote);
    }

    GlobalSettings settings;

    List<SimpleNote> actualNotes;

    private void CreateNoteVertically(int position, float startTime, float length, SimpleNote actualMidiNote) {
        // MIDI note 21 is the A0 on a keyboard --> https://newt.phys.unsw.edu.au/jw/notes.html
        float width = noteBackground.GetComponent<RectTransform>().rect.width;
        int totalNotes = 108 - 20;
        int actualPosition = position - 24;
        float widthPerOctave = width / (float) (settings.numberOfOctaves);
        float widthPerNote = widthPerOctave / (float) WhiteKeysPerOctave;

        // between 0 and 11 now
        int noteInOctave = actualPosition % TotalKeysPerOctave;
        int octave = (int) (actualPosition / TotalKeysPerOctave);

        float distance = 0;
        if (noteInOctave == 1) {
            distance = 0.5f;
        } else if (noteInOctave == 2) {
            distance = 1f;
        } else if (noteInOctave == 3) {
            distance = 1.5f;
        } else if (noteInOctave == 4) {
            distance = 2f;
        } else if (noteInOctave == 5) {
            distance = 3f;
        } else if (noteInOctave == 6) {
            distance = 3.5f;
        } else if (noteInOctave == 7) {
            distance = 4f;
        } else if (noteInOctave == 8) {
            distance = 4.5f;
        } else if (noteInOctave == 9) {
            distance = 5f;
        } else if (noteInOctave == 10) {
            distance = 5.5f;
        } else if (noteInOctave == 11) {
            distance = 6f;
        }

        GameObject newNote = Instantiate(note);
        noteImages.Add(newNote);
        float height = noteBackground.GetComponent<RectTransform>().rect.height;

        newNote.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(
            widthPerNote * distance
            + widthPerOctave * (octave - settings.startOctave)
            + widthPerNote / 2,
            height / timeframe * startTime - height / timeframe * timePassed,
            newNote.GetComponent<RectTransform>().position.z
        );

        newNote.transform.SetParent(noteBackground.transform, false);
        if (
            noteInOctave == 1
            || noteInOctave == 3
            || noteInOctave == 6
            || noteInOctave == 8
            || noteInOctave == 10
        ) {
            newNote.GetComponent<RectTransform>().sizeDelta = new Vector2(
                widthPerNote / 2,
                height / timeframe * length
            );
            newNote.GetComponent<Image>().color = Color.blue;
        } else {
            newNote.GetComponent<RectTransform>().sizeDelta = new Vector2(
                widthPerNote,
                height / timeframe * length
            );
        }

        // set note name

        newNote.transform.GetChild(0).GetComponent<Text>().text = noteToString(noteInOctave);
        newNote.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition3D =
            new Vector3(0f, 9.5f, note.GetComponent<RectTransform>().anchoredPosition3D.z);
    }

    private void CreateNoteHorizontally(int noteNumber, float noteStartTime, float noteLength) {
        float height = noteBackground.GetComponent<RectTransform>().rect.height;
        int actualPosition = noteNumber - 24;
        float heightPerOctave = height / settings.numberOfOctaves;
        float heightPerNote = heightPerOctave / WhiteKeysPerOctave;

        int noteInOctave = actualPosition % TotalKeysPerOctave;
        int octave = actualPosition / TotalKeysPerOctave;

        float distance = CalculateNoteDistance(noteInOctave);

        GameObject newNote = Instantiate(note);
        noteImages.Add(newNote);
        float width = noteBackground.GetComponent<RectTransform>().rect.width;

        //TODO Berechnung prüfen (Noten spawnen falsch)
        /*
        newNote.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(
            width / timeframe * startTime - width / timeframe * timePassed,
            heightPerNote * distance
            + heightPerOctave * (settings.startOctave - octave)
            + heightPerNote / 2,
            newNote.GetComponent<RectTransform>().position.z
        ); */
        newNote.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(
            width / timeframe * (noteStartTime - timePassed), // Berechnung der horizontalen Bewegung
            heightPerNote * distance
            + heightPerOctave * (octave - settings.startOctave)
            + heightPerNote / 2,
            newNote.GetComponent<RectTransform>().position.z
        );

        newNote.transform.SetParent(noteBackground.transform, false);
        if (noteInOctave == 1
            || noteInOctave == 3
            || noteInOctave == 6
            || noteInOctave == 8
            || noteInOctave == 10) {
            newNote.GetComponent<RectTransform>().sizeDelta = new Vector2(
                width / timeframe * noteLength,
                heightPerNote / 2
            );
            newNote.GetComponent<Image>().color = Color.blue;
        } else {
            newNote.GetComponent<RectTransform>().sizeDelta = new Vector2(
                width / timeframe * noteLength,
                heightPerNote
            );
        }
        newNote.transform.GetChild(0).GetComponent<Text>().text = noteToString(noteInOctave);
        newNote.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition3D =
            new Vector3(0f, 0f, note.GetComponent<RectTransform>().anchoredPosition3D.z);
    }

    private float CalculateNoteDistance(int noteInOctave) {
        return noteInOctave switch {
            <= 0 => 0,
            <= 4 => noteInOctave * 0.5f,
            //Add Extra Distance since there is no Black Key between E and F
            _ => (noteInOctave * 0.5f) + 0.5f
        };
    }


    public string noteToString(int noteNumber) {
        switch (noteNumber) {
            case 0:
                return "C";
            case 1:
                return "C#";
            case 2:
                return "D";
            case 3:
                return "D#";
            case 4:
                return "E";
            case 5:
                return "F";
            case 6:
                return "F#";
            case 7:
                return "G";
            case 8:
                return "G#";
            case 9:
                return "A";
            case 10:
                return "A#";
            case 11:
                return "B";
        }

        return "";
    }
}
