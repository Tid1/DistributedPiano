using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;

public class SimpleNote
{
    /*
    Created by Marvin Krueger
    This file was created by us. Intially used for another project and adjust for the purpose of this project
    */
    public float startTime = 0f;
    public float duration = 0f;
    public int noteNumber;
    public NoteName noteName;

    public SimpleNote(float startTime, float duration, int noteNumber)
    {
        this.startTime = startTime;
        this.duration = duration;
        this.noteNumber = noteNumber;
    }

    public SimpleNote() { }
}
