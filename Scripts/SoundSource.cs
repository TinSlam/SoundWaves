using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class SoundSource{
    public int x;
    public int y;
    public float period;
    public float amplitude;
    public float initialPhase;

    public Vector2[] partialDirections;

    private SoundSource mainSource;

    private SoundWaves soundWaves;

    public float omega;

    public float[,] cells;

    public List<Particle>[,] particles;
    public Vector2[,] velocityField;

    public int particleCount;

    public bool disabled = false;

    public List<SoundSource> diffractionSources = new List<SoundSource>();

    private static float squareOfTwoInverse = 1 / Mathf.Sqrt(2);

    public class Particle{
        public float amplitudeLoss;
        public float phase;
        public Vector2 direction;
        public bool isNew;

        public Particle(float amplitudeLoss, float phase, Vector2 direction){
            this.amplitudeLoss = amplitudeLoss;
            this.phase = phase;
            this.direction = direction;
            isNew = true;
        }
    };

    public SoundSource(int x, int y, float period, float amplitude, float initialPhase, Vector2[] partialDirections, float[,] cells, SoundSource mainSource, SoundWaves soundWaves){
        this.x = x;
        this.y = y;
        this.period = period;
        this.amplitude = amplitude;
        this.initialPhase = initialPhase;
        this.partialDirections = partialDirections;
        this.cells = cells;
        this.mainSource = mainSource;
        this.soundWaves = soundWaves;

        omega = 2 * Mathf.PI / period;

        particles = new List<Particle>[soundWaves.height, soundWaves.width];
        for(int i = 0; i < soundWaves.height; i++)
            for(int j = 0; j < soundWaves.width; j++)
                particles[i, j] = new List<Particle>();

        velocityField = new Vector2[soundWaves.height, soundWaves.width];
        updateVelocityField();
    }

    public void updateVelocityField(){
        for(int i = 0; i < soundWaves.height; i++)
            for(int j = 0; j < soundWaves.width; j++)
                if(Utils.isInVision(x, y, i, j, cells))
                    velocityField[i, j] = (new Vector2(i, j) - new Vector2(x, y)).normalized;
                //if(i < 23)
                //    velocityField[i, j] = new Vector2(1, 0).normalized;
                //else
                //    velocityField[i, j] = ((new Vector2(i, j) - new Vector2(x, y)).normalized).normalized;
                else
                    velocityField[i, j] = Vector2.one;
    }

    public bool updateWave(float phaseOffset, bool oneTime, bool stopAdding){
        if(!disabled && !stopAdding)
            addSource(phaseOffset);

        if(!stopAdding)
            propagate();
        
        compute(phaseOffset);

        if(mainSource == null){
            for(int i = 0; i < diffractionSources.Count; i++)
                diffractionSources[i].updateWave(diffractionSources[i].initialPhase, true, stopAdding);

            for(int i = 0; i < diffractionSources.Count; i++)
                if(diffractionSources[i].particleCount == 0){
                    diffractionSources.RemoveAt(i);
                    i--;
                }else
                    particleCount += diffractionSources[i].particleCount;
        }

        if(oneTime)
            disabled = true;

        if(mainSource == null && disabled && particleCount == 0)
            return true;

        return false;
    }

    private void addSource(float phase){
        Particle particle = new Particle(0, phase - initialPhase, Vector2.zero);
        particle.isNew = false;
        particles[x, y].Add(particle);
    }

    private void propagate(){
        for(int i = 0; i < soundWaves.height; i++)
            for(int j = 0; j < soundWaves.width; j++)
                foreach(Particle particle in particles[i, j])
                    if(!particle.isNew)
                        spread(i, j, particle);
    }

    private void spread(int i, int j, Particle particle){
        float newAmplitudeLoss = particle.amplitudeLoss + soundWaves.decayingFactor * soundWaves.cellSize;

        if(newAmplitudeLoss >= amplitude)
            return;

        float newPhase = particle.phase - soundWaves.cellSize * soundWaves.speedOfSoundInv;

        spread(i + 1, j, i, j, newAmplitudeLoss, newPhase, particle.direction);
        spread(i - 1, j, i, j, newAmplitudeLoss, newPhase, particle.direction);
        spread(i, j + 1, i, j, newAmplitudeLoss, newPhase, particle.direction);
        spread(i, j - 1, i, j, newAmplitudeLoss, newPhase, particle.direction);
    }

    private void spread(int i, int j, int sx, int sy, float amplitudeLoss, float phase, Vector2 sourceDirection){
        if(i < 0 || j < 0 || i >= soundWaves.height || j >= soundWaves.width || cells[i, j] == float.MinValue || velocityField[i, j] == Vector2.one)
            return;

        if(partialDirections.Length != 0){
            //Debug.DrawRay(new Vector2(sy + 0.5f, sx + 0.5f), partialDirections[0], Color.green);
            //Debug.DrawRay(new Vector2(sy + 0.5f, sx + 0.5f), partialDirections[1], Color.magenta);
            //Debug.DrawRay(new Vector2(sy + 0.5f, sx + 0.5f), new Vector2(-1, 0), Color.yellow);
            //Debug.Break();
            if(Utils.isBetween(new Vector2(i - sx, j - sy), partialDirections[0], partialDirections[1]))
                return;
        }

        if(sourceDirection == Vector2.zero)
            particles[i, j].Add(new Particle(amplitudeLoss, phase, velocityField[i, j]));
        else{
            float dot = Vector2.Dot(velocityField[i, j], new Vector2(i - sx, j - sy));
            if(dot <= 0)
                return;

            dot = Vector2.Dot(sourceDirection, velocityField[i, j]);
            if(dot <= 0)
                return;

            //bool debug = i == 10 && j == 10;
            //if(debug)
            //    Debug.Log(dot * amplitudeLoss + " " + dot);

            //float angle = Mathf.Atan2(j - y, i - x);
            //float scale = (1 - Mathf.Cos(4 * angle)) / 2;
            //dot += amplitudeLoss * scale * 1000000;

            //if(i == sx || j == sy)
                dot = 1;
            //else
                //dot = 1.4f;

            float loss = amplitudeLoss * dot;
            if(loss >= amplitude)
                return;

            Particle particle = new Particle(loss, phase, velocityField[i, j]);
            particles[i, j].Add(particle);

            //bool debug = i == 7 && j == 8;
            //if(debug) {
            //    Debug.Log(loss + " " + i + " " + j + " " + sx + " " + sy + " " + Vector2.Dot(velocityField[i, j], new Vector2(i - sx, j - sy)) + " " + velocityField[i, j]);
            //    Debug.Break();
            //}

            diffract(i, j, particle);
        }

        //if(partialDirections.Length != 0)
        //    soundWaves.tempColoring.Add(new int[] { i, j });
    }

    private void diffract(int i, int j, Particle particle){
        SoundWaves.DiffractionCorner corner;
        
        if(soundWaves.diffractionCorners.TryGetValue(new Vector2(i, j), out corner))
            diffract(corner, particle);
    }

    private void diffract(SoundWaves.DiffractionCorner corner, Particle particle){
        if(Vector2.Dot(particle.direction, corner.cornerDirection) > 0)
            return;

        //Debug.Log(amplitude - particle.amplitudeLoss);

        if(mainSource == null)
            diffractionSources.Add(new SoundSource(corner.x, corner.y, period, amplitude - particle.amplitudeLoss, particle.phase, new Vector2[]{particle.direction, corner.cornerDirection}, soundWaves.cloneCells(), mainSource == null ? this : mainSource, soundWaves));
        else    
            mainSource.diffractionSources.Add(new SoundSource(corner.x, corner.y, period, amplitude - particle.amplitudeLoss, particle.phase, new Vector2[]{particle.direction, corner.cornerDirection}, soundWaves.cloneCells(), mainSource == null ? this : mainSource, soundWaves));
    }

    private void compute(float phaseOffset){
        particleCount = 0;

        for(int i = 0; i < soundWaves.height; i++)
            for(int j = 0; j < soundWaves.width; j++){
                if(cells[i, j] == float.MinValue)
                    continue;

                //bool debug = i == 3 && j == 9 && cells[i, j] != 0;

                cells[i, j] = 0;
                List<Particle> pList = particles[i, j];
                for(int k = 0; k < pList.Count; k++){
                    Particle particle = pList[k];
                    if(particle.isNew){
                        particleCount++;
                        //if(debug) {
                        //    Debug.Log((amplitude - particle.amplitudeLoss) + " " + Mathf.Cos(omega * particle.phase));
                        //}
                        cells[i, j] += (amplitude - particle.amplitudeLoss) * Mathf.Cos(omega * particle.phase);
                        particle.isNew = false;
                    }else
                        pList.RemoveAt(k--);
                }

                int count = pList.Count;

                for(int m = 0; m < diffractionSources.Count; m++){
                    pList = diffractionSources[m].particles[i, j];
                    count += pList.Count;
                    cells[i, j] += diffractionSources[m].cells[i, j];
                }

                if(mainSource == null && count != 0)
                    cells[i, j] /= count;

                //if(cells[i, j] < -100)
                //    Debug.Log(cells[i, j] + " " + pList.Count);
                
                //if(debug && cells[i, j] != 0){
                //    Debug.Log("Lol! " + cells[i, j] + " " + x + " " + y);
                //    Debug.Break();
                //}
            }

        if(/*!disabled && */mainSource == null && !disabled)
            cells[x, y] += amplitude * Mathf.Cos(phaseOffset);
        
        //if(cells[3, 9] != 0)
        //    Debug.Log(cells[3, 9]);
    }
}
