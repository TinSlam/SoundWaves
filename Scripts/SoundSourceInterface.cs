using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundSourceInterface: MonoBehaviour{
    public float amplitude;
    public float period;
    public float initialPhase;

    public float particleCount;

    public SoundSource soundSource;
    
    private void Update(){
        soundSource.amplitude = amplitude;
        soundSource.period = period;
        soundSource.initialPhase = initialPhase;
        soundSource.omega = 2 * Mathf.PI / period;
        particleCount = soundSource.particleCount;
    }
}
