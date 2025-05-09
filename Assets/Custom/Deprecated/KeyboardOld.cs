using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class KeyboardOld : MonoBehaviour
{
    public GameObject blackTile, whiteTile;
    public GameObject content;

    private void Start() {
        Application.targetFrameRate = 120;
        settings = this.GetComponent<GlobalSettings>();
        CreateKeyboardUI();
    }

    GlobalSettings settings;
    public Image gridLines; 
    
    public void CreateKeyboardUI(){
        gridLines.material.SetInt("numberOctaves", settings.numberOfOctaves); 

        foreach (Transform item in content.transform)
        {
            Destroy(item.gameObject); 
        }
        int startNote = 24;
        for(int i = 0; i < settings.numberOfOctaves; i++){
            createOctave(startNote+i*12+settings.startOctave*12, i+settings.startOctave); 
        }
    }

    private void createOctave(int startNote, int octave){
        // Overall width
        float width = content.GetComponent<RectTransform>().rect.width;
        float widthPerOctave = width / settings.numberOfOctaves;
        float widthPerNote = widthPerOctave / 7; 

        
        // We have 7 white keys
        for(int i = 0; i < 7; i++){
            int actualNote = getWhiteKeyIndex(i);
            GameObject c = instantiateNote(whiteTile, actualNote, startNote);
            registerEvents(c);

            c.GetComponent<RectTransform>().sizeDelta = new Vector2(widthPerNote-1, c.GetComponent<RectTransform>().sizeDelta.y);
            c.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(widthPerOctave*(octave-settings.startOctave)+widthPerNote*i + widthPerNote/2, -whiteTile.GetComponent<RectTransform>().rect.height/2, 0);
        }

        // We have 5 black keys
        for(int i = 0; i < 5; i++){
            // Get the note index
            int actualNote = getBlackKeyIndex(i);

            // Create note from preset
            GameObject c = instantiateNote(blackTile, actualNote, startNote);

            // Register key up and down event
            registerEvents(c);


            c.GetComponent<RectTransform>().sizeDelta = new Vector2(widthPerNote/2, c.GetComponent<RectTransform>().sizeDelta.y);
            int blackIndex = i; 
            if(i > 1){
                blackIndex += 1;
            }
            c.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(widthPerOctave*(octave-settings.startOctave)+widthPerNote*blackIndex + widthPerNote, -blackTile.GetComponent<RectTransform>().rect.height/2, 0);
        }
    }

    private GameObject instantiateNote(GameObject notePreset, int actualNote, int startNote){
        GameObject note = Instantiate(notePreset); 
        note.transform.SetParent(content.transform, false);         
        note.GetComponent<PianoTile>().midiNote = startNote + actualNote;
        return note;
    }

    private int getWhiteKeyIndex(int i){
        int actualNote = 0; 
        if(i == 1){
            //D
            actualNote = 2; 
        } else if(i == 2){
            // E
            actualNote = 4; 
        } else if(i == 3){
            //F
            actualNote = 5; 
        }else if(i == 4){
            //G
            actualNote = 7; 
        }
        else if(i == 5){
            //A
            actualNote = 9; 
        }
        else if(i == 6){
            //A
            actualNote = 11; 
        }
        return actualNote;
    }

    private int getBlackKeyIndex(int i){
        // CS
        int actualNote = 1; 
        if(i == 1){
            // DS
            actualNote = 3; 
        } else if(i == 2){
            // ES
            actualNote = 6; 
        } else if(i == 3){
            //F
            actualNote = 8; 
        }else if(i == 4){
            //F
            actualNote = 10; 
        }
        return actualNote;
    }


    private void registerEvents(GameObject note){
        EventTrigger trigger = note.gameObject.AddComponent<EventTrigger>();
        var pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerEnter;
        pointerDown.callback.AddListener((e) =>  clickedTile(note.GetComponent<PianoTile>().midiNote, note));
        trigger.triggers.Add(pointerDown);

        var pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerExit;
        pointerUp.callback.AddListener((e) =>  keyOff(note.GetComponent<PianoTile>().midiNote, note));
        trigger.triggers.Add(pointerUp);
    }

    public void clickedTile(int midiNote, GameObject note){
        network.OnKey(midiNote);
        note.GetComponent<Image>().color = Color.green;
        Camera.main.GetComponent<NoteVisualizer>().keyOn(midiNote);
    }

    public OSCNetwork network;

    public void keyOff(int midiNote, GameObject note){
        if(note.gameObject.name.Contains("White")){
            note.GetComponent<Image>().color = Color.white;
        } else {
            note.GetComponent<Image>().color = Color.black;
        }
        network.OffKey(midiNote);
        Camera.main.GetComponent<NoteVisualizer>().keyOff(midiNote);
    }
}
