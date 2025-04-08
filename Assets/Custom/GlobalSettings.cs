using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalSettings : MonoBehaviour
{
    public int numberOfOctaves = 3;
    public int startOctave = 0;


    private void Awake() {
        #if UNITY_IOS
        numberOfOctaves = 2;
        startOctave = 3;

        #endif

        #if UNITY_EDITOR
        numberOfOctaves = 3;
        startOctave = 0;
        #endif
    }
}
