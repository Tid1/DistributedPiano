using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDrawingScript : MonoBehaviour {
    [SerializeField] private Keyboard keyboard;
    [SerializeField] private SheetMusicVisualization visualization;
    
    // Start is called before the first frame update
    void Start() {
        keyboard.OnKeyOn += visualization.OnKeyOn;
        keyboard.OnKeyOff += visualization.OnKeyOff;
    }
    
}
