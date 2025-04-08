using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeMeasurementType : MonoBehaviour
{
        /* 
    Created by Marvin Krueger
    This file was created by us. Intially used for another project and adjust for the purpose of this project
     */
    public static TimeMeasurementType instance;
    public enum TimeMeasurement{
        Christian, 
        SNTP, 
        StartSignal
    }

    public TimeMeasurement type = TimeMeasurement.StartSignal;
    void Start()
    {
        TimeMeasurementType.instance = this;
        this.GetComponent<Text>().text = "Start Signal";

        this.GetComponent<Button>().onClick.AddListener(delegate{changeTimeMeasurement();});
    }

    public void changeTimeMeasurement(){
        if(TimeMeasurement.Christian == type){
            type = TimeMeasurement.SNTP; 
            this.GetComponent<Text>().text = "SNTP";
        } else if(TimeMeasurement.SNTP == type){
            type = TimeMeasurement.StartSignal;
            this.GetComponent<Text>().text = "Start Signal";
        } else {
            type = TimeMeasurement.Christian;
            this.GetComponent<Text>().text = "Christian";
        }
    }
}
