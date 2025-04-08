using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Util 
{
    public static long ElapsedNanoSeconds(Stopwatch watch)
    {
        return watch.ElapsedTicks * 1000000000 / Stopwatch.Frequency;
    }
    public static long ElapsedMicroSeconds(Stopwatch watch)
    {
        return watch.ElapsedTicks * 1000000 / Stopwatch.Frequency;
    }
}
