using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitySoundGen : MonoBehaviour
{
    /*
    Created by Marvin Krueger
    This file was created by us. We used a method for sound generation which was intially mentioned here: https://www.youtube.com/watch?v=GqHFGMy_51c and adjusted it for our purposes
     */
    float sampleRate;
    AudioSource audioSource;

    public Dictionary<int, List<float>> frequencies;

    List<float> phase,
        increment;

    void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
        phase = new List<float> { 0, 0 };
        increment = new List<float> { 0, 0 };
        frequencies = new Dictionary<int, List<float>>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = true;
    }

    public void OnKey(int keyNumber)
    {
        // https://www.inspiredacoustics.com/en/MIDI_note_numbers_and_center_frequencies
        float freq = 440 * Mathf.Pow(2, ((float)keyNumber - 69f) / 12f);
        frequencies[keyNumber] = new List<float> { freq, (float)AudioSettings.dspTime };
    }

    public void changePitch(int keyNumber, float pitch)
    {
        try
        {
            frequencies[keyNumber][1] = pitch;
        }
        catch
        {
            Debug.Log("Not yet here");
        }
    }

    public void onKeyOff(int keyNumber)
    {
        frequencies.Remove(keyNumber);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
            else
            {
                audioSource.Stop();
            }
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        // General idea from here --> https://www.youtube.com/watch?v=GqHFGMy_51c
        int counter = 0;
        try
        {
            foreach (var item in frequencies.Keys)
            {
                for (int i = 0; i < data.Length; i += channels)
                {
                    float freq = frequencies[item][0];
                    //data[i] = generateSample(freq, i / channels);
                    //float vibratoAmount = frequencies[item][1];
                    //float incrementAmount = (freq) * 2f * Mathf.PI/ sampleRate;
                    //float incrementAmount = (i) * 2f * Mathf.PI/ freq;
                    //phase[counter] += incrementAmount;
                    /*if(phase[counter] > (Mathf.PI*2f)){
                        phase[counter] = 0;
                    }*/
                    //data[i] = (float) (Mathf.Sin(phase[counter]));
                }
                counter++;
            }

            foreach (var item in frequencies.Keys)
            {
                int currentDataStep = 0;
                float freq = frequencies[item][0];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] +=
                        calculateSound(
                            currentDataStep,
                            freq,
                            frequencies[item][1]
                        ) * 0.1f;
                    currentDataStep++;
                    // Stereo
                    if (channels == 2)
                    {
                        data[i + 1] = data[i]; 
                        i++;
                    }
                }
            }
        }
        catch
        {
            Debug.Log("Accesing while changing the frequency");
        }
    }

    private const float PI_2 = 2 * Mathf.PI;
    private Dictionary<float, float> Phases;

    public float[] harmonicStrengths = new float[]
    {
        0.25f,
        0.5f,
        0.25f,
        0.1f,
        0.05f,
        0.0025f,
        0.001f,
        0.0005f,
        0.00025f,
        0.0001f,
        0.0001f,
        0.0001f
    };

    public float calculateSound(int dataIndex, float freq, double audioTime)
    {
        // Inspired from this project: https://github.com/IRCSS/Procedural-Sound/
        float harmonic = 0.0f;

        for (int i = 1; i <= 12; i++)
        {
            float harmonicFrequency = freq * i;

            float timeAtTheBeginig = (float)(
                (AudioSettings.dspTime - audioTime) % (1.0 / (double)harmonicFrequency)
            ); 

            float exactTime = timeAtTheBeginig + (float)dataIndex / sampleRate;

            harmonic +=
                Mathf.Sin(exactTime * harmonicFrequency * 2f * Mathf.PI) * harmonicStrengths[i - 1];
            ;
        }

        return harmonic;
    }

    private float GetNextSample()
    {
        if (Phases == null)
        {
            Phases = new Dictionary<float, float>();
        }
        float sineval = 0;

        foreach (var item in frequencies.Keys)
        {
            float freq = frequencies[item][0];
            if (!Phases.ContainsKey(freq))
            {
                Phases[freq] = 0f;
            }
            Phases[freq] += freq * PI_2 / sampleRate;
            Phases[freq] %= PI_2;
            sineval += Mathf.Sin(Phases[freq]) * 0.3f;
        }
        if (sineval > 1)
        {
            return 1;
        }
        else if (sineval < -1)
        {
            return -1;
        }
        return sineval;
    }

    public float generateSample(float freq, float sampleNr)
    {
        float angFreq = freq * 2 * Mathf.PI;
        float sampleTime = sampleNr / sampleRate;
        float sampleAngle = sampleTime * angFreq;
        return Mathf.Sin(sampleAngle);
    }
}
