using System.Collections;
using System.Collections.Generic;
using System.IO;
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

public class MIDIParser : MonoBehaviour
{
    public MidiFile midiFile;
    public TempoMap tempoMap;
    public List<SimpleNote> notes;


    public void loadMidiFile()
    {
        // Open file with filter
#if UNITY_STANDALONE || UNITY_EDITOR
        var extensions = new[] {
            new ExtensionFilter("MIDI file", "midi", "mid")
        };
        var path = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
        if (path.Length > 0)
        {
            loadFile(path[0]);
        }
#else
        NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
           {
               if (path == null)
                   Debug.Log("Operation cancelled");
               else
                   Debug.Log("Picked file: " + path);
                   loadFile(path);
           }, new string[] { NativeFilePicker.ConvertExtensionToFileType("mid"), NativeFilePicker.ConvertExtensionToFileType("midi")});

        Debug.Log("Permission result: " + permission);

#endif

    }

    public void loadFile(string filePath)
    {
        Debug.Log("loading midi file" + filePath);
        midiFile = MidiFile.Read(filePath);
        tempoMap = midiFile.GetTempoMap();
        List<Note> allNotes = midiFile.GetNotes().ToList();
        notes = new List<SimpleNote>();
        foreach (var item in allNotes)
        {
            SimpleNote newNote = new SimpleNote();
            newNote.startTime = item.TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000000f;
            newNote.duration = item.LengthAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000000f;
            newNote.noteNumber = item.NoteNumber;
            notes.Add(newNote);
        }
    }

    public void LoadFile(string fileName, MemoryStream midiStream) {
        Debug.Log("loading midi file" + fileName);
        midiFile = MidiFile.Read(midiStream);
        tempoMap = midiFile.GetTempoMap();
        List<Note> allNotes = midiFile.GetNotes().ToList();
        notes = new List<SimpleNote>();
        foreach (var item in allNotes)
        {
            SimpleNote newNote = new SimpleNote();
            newNote.startTime = item.TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000000f;
            newNote.duration = item.LengthAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000000f;
            newNote.noteNumber = item.NoteNumber;
            notes.Add(newNote);
        } 
    }


    // Start is called before the first frame update
    void Start()
    {
        var db = Resources.Load<TextAsset>("example");
        byte[] data = db.bytes;
        System.IO.File.WriteAllBytes(Application.persistentDataPath + "/example.midi", data);
        midiFile = MidiFile.Read(Application.persistentDataPath + "/example.midi");
        tempoMap = midiFile.GetTempoMap();
        List<Note> allNotes = midiFile.GetNotes().ToList();
        notes = new List<SimpleNote>();
        foreach (var item in allNotes)
        {
            SimpleNote newNote = new SimpleNote();
            newNote.startTime = item.TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000000f;
            newNote.duration = item.LengthAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000000f;
            newNote.noteNumber = item.NoteNumber;
            notes.Add(newNote);
        }
    }
}
