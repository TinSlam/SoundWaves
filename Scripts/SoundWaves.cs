using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundWaves: MonoBehaviour{
    public int width;
    public int height;
    public int cellSize;
    
    public float speedOfSound;
    [HideInInspector] public float speedOfSoundInv;

    public float decayingFactor;

    public float simulationStepDelay;
    private float time = 0;

    public float maxAmplitudeColorScale;

    private SpriteRenderer[,] tiles;
    private float[,] cells;
    public Dictionary<Vector2, DiffractionCorner> diffractionCorners;
    public List<int[]> tempColoring;

    private GameObject soundSourcesObject;
    private GameObject particlesText;
    private GameObject tooManyParticlesText;

    public struct DiffractionCorner{
        public int x;
        public int y;
        public Vector2 cornerDirection;

        public DiffractionCorner(int x, int y, Vector2 cornerDirection){
            this.x = x;
            this.y = y;
            this.cornerDirection = cornerDirection.normalized;
        }
    };

    [HideInInspector] public List<SoundSourceInterface> sources = new List<SoundSourceInterface>();

    public void Start(){
        soundSourcesObject = GameObject.Find("SoundSources");
        particlesText = GameObject.Find("ParticleCount");
        tooManyParticlesText = GameObject.Find("TooManyParticles");

        initializeCells();
        centerCamera();
        updateCellColors();
        addSource(height / 2, width / 2, 4 * cellSize, 10, 0, new Vector2[0]);
    }

    public void addSource(int x, int y, float period, float amplitude, float initialPhase, Vector2[] partialDirection){
        GameObject newSource = new GameObject("SoundSource");
        newSource.transform.parent = soundSourcesObject.transform;
        SoundSourceInterface sourceInterface = newSource.AddComponent<SoundSourceInterface>();
        sourceInterface.soundSource = new SoundSource(x, y, period, amplitude, initialPhase, partialDirection, cloneCells(), null, this);
        sourceInterface.amplitude = amplitude;
        sourceInterface.period = period;
        sourceInterface.initialPhase = initialPhase;
        sources.Add(sourceInterface);
    }

    private int particleCount = 0;

    public void Update(){
        if(speedOfSound == 0)
            return;

        speedOfSoundInv = 1 / speedOfSound;
        
        pollEvents();

        if(Time.realtimeSinceStartup - time < simulationStepDelay)
            return;

        time = Time.realtimeSinceStartup;

        tempColoring.Clear();

        bool stopAdding = particleCount > 50000;
        //tooManyParticlesText.SetActive(stopAdding);

        updateWave(stopAdding);
        updateCellColors();
    }

    private void pollEvents(){
        if(Input.GetMouseButtonDown(0)){
            Vector2 m = Utils.getMousePosition();
            int j = (int) m.x;
            int i = (int) m.y;
            
            if(j < 0 || j > width - 1 || i < 0 || i > height - 1)
                goto end;

            toggleCells(i, j, cells);

            foreach(SoundSourceInterface sourceInterface in sources){
                toggleCells(i, j, sourceInterface.soundSource.cells);
                sourceInterface.soundSource.updateVelocityField();

                foreach(SoundSource diffractionSource in sourceInterface.soundSource.diffractionSources){
                    toggleCells(i, j, diffractionSource.cells);
                    diffractionSource.updateVelocityField();
                }
            }
        }

        if(Input.GetMouseButtonDown(1)){
            Vector2 m = Utils.getMousePosition();
            int j = (int) m.x;
            int i = (int) m.y;

            if(j < 0 || j > width - 1 || i < 0 || i > height - 1)
                goto end;

            for(int k = 0; k < sources.Count; k++)
                if(sources[k].soundSource.x == i && sources[k].soundSource.y == j){
                    sources[k].soundSource.disabled = true;
                    goto end;
                }

            addSource(i, j, 4 * cellSize, 10, 0, new Vector2[0]);
        }

        end:
            updateDiffractionCorners();
    }

    private void toggleCells(int i, int j, float[,] cells){
        if(cells[i, j] == float.MinValue)
            cells[i, j] = 0;
        else
            cells[i, j] = float.MinValue;
    }

    private int counter = 0;

    private void updateWave(bool stopAdding){
        for(int i = 0; i < sources.Count; i++)
            if(sources[i].soundSource.updateWave(counter / sources[i].period / 2, false, stopAdding)){
                Destroy(sources[i].gameObject);
                sources.RemoveAt(i);
                i--;
            }

        counter++;

        for(int i = 0; i < height; i++)
            for(int j = 0; j < width; j++){
                if(cells[i, j] == float.MinValue)
                    continue;

                float value = 0;

                for(int k = 0; k < sources.Count; k++)
                    value += sources[k].soundSource.cells[i, j];

                cells[i, j] = value;
            }

        particleCount = 0;
        
        foreach(SoundSourceInterface sourceInterface in sources)
            particleCount += sourceInterface.soundSource.particleCount;

        particlesText.GetComponent<Text>().text = "Particle Count: " + particleCount;
    }

    private void updateCellColors(){
        //if(sources.Count == 0)
            //return;

        for(int i = 0; i < height; i++)
            for(int j = 0; j < width; j++)
                if(cells[i, j] == float.MinValue)
                    tiles[i, j].color = Color.red;
                else
                    tiles[i, j].color = Color.LerpUnclamped(Color.cyan, Color.blue, cells[i, j] / maxAmplitudeColorScale / 2 + 0.5f);

        //foreach(int[] tile in tempColoring)
        //    tiles[tile[0], tile[1]].color = Color.LerpUnclamped(Color.blue, Color.green, cells[tile[0], tile[1]] / maxAmplitudeColorScale / 2 + 0.5f);

        //foreach(KeyValuePair<Vector2, DiffractionCorner> pair in diffractionCorners)
        //tiles[pair.Value.x, pair.Value.y].color = new Color(0, 0, 1, 0.2f);

        //for(int i = 0; i < height; i++)
        //    for(int j = 0; j < width; j++)
        //        if(sources[0].velocityField[i, j] == Vector2.one)
        //            tiles[i, j].color = Color.blue;
    }

    private void initializeCells(){
        cells = new float[height, width];
        tiles = new SpriteRenderer[height, width];
        diffractionCorners = new Dictionary<Vector2, DiffractionCorner>();
        tempColoring = new List<int[]>();

        GameObject tilesObject = new GameObject("Tiles");
        tilesObject.transform.parent = transform;

        for(int i = 0; i < height; i++)
            for(int j = 0; j < width; j++){
                cells[i, j] = 0;
                GameObject tile = Instantiate(Resources.Load("Prefabs/Tile") as GameObject);
                tile.name = "Tile";
                tile.transform.parent = tilesObject.transform;
                tile.transform.position = new Vector2(j * cellSize, i * cellSize) + Vector2.one * cellSize / 2.0f;
                tile.transform.localScale = new Vector2(cellSize, cellSize);
                tiles[i, j] = tile.GetComponent<SpriteRenderer>();
            }

        //cells[2, 3] = float.MinValue;
        //cells[3, 3] = float.MinValue;
        //cells[3, 2] = float.MinValue;
        //cells[4, 2] = float.MinValue;
        //cells[5, 2] = float.MinValue;

        //cells[2, 4] = float.MinValue;
        //cells[2, 5] = float.MinValue;
        //cells[2, 6] = float.MinValue;
        //cells[2, 7] = float.MinValue;
        //cells[2, 8] = float.MinValue;

        //cells[7, 3] = float.MinValue;

        updateDiffractionCorners();

        //diffractionCorners.Add(new DiffractionCorner(6, 3, new Vector2(-1, -1)));
        //diffractionCorners.Add(new DiffractionCorner(6, 1, new Vector2(-1, 1)));
        //diffractionCorners.Add(new DiffractionCorner(2, 1, new Vector2(1, 1)));
        //diffractionCorners.Add(new DiffractionCorner(1, 2, new Vector2(1, 1)));
        //diffractionCorners.Add(new DiffractionCorner(1, 9, new Vector2(1, -1)));
        //diffractionCorners.Add(new DiffractionCorner(3, 9, new Vector2(-1, -1)));
        //diffractionCorners.Add(new DiffractionCorner(4, 4, new Vector2(-1, -1)));
    }

    private void updateDiffractionCorners(){
        diffractionCorners.Clear();

        for(int i = 1; i < height - 1; i++)
            for(int j = 1; j < width - 1; j++){
                Vector2 direction;
                
                if(isDiffractionCorner(i, j, out direction))
                    diffractionCorners.Add(new Vector2(i, j), new DiffractionCorner(i, j, direction));
            }
    }

    private bool isDiffractionCorner(int i, int j, out Vector2 direction){
        if(cells[i - 1, j - 1] == float.MinValue && cells[i, j - 1] != float.MinValue && cells[i - 1, j] != float.MinValue){
            direction = new Vector2(-1, -1);
            return true;
        }

        if(cells[i - 1, j + 1] == float.MinValue && cells[i, j + 1] != float.MinValue && cells[i - 1, j] != float.MinValue){
            direction = new Vector2(-1, 1);
            return true;
        }

        if(cells[i + 1, j - 1] == float.MinValue && cells[i, j - 1] != float.MinValue && cells[i + 1, j] != float.MinValue){
            direction = new Vector2(1, -1);
            return true;
        }

        if(cells[i + 1, j + 1] == float.MinValue && cells[i, j + 1] != float.MinValue && cells[i + 1, j] != float.MinValue){
            direction = new Vector2(1, 1);
            return true;
        }

        direction = Vector2.zero;
        return false;
    }

    private void centerCamera(){
        Camera.main.orthographicSize = cellSize * (height + 2) / 2.0f;
        Camera.main.transform.position = new Vector3(width * cellSize / 2.0f, height * cellSize / 2.0f, Camera.main.transform.position.z);
    }

    public float[,] cloneCells(){
        float[,] newCells = new float[height, width];
        for(int i = 0; i < height; i++)
            for(int j = 0; j < width; j++)
                if(cells[i, j] == float.MinValue)
                    newCells[i, j] = float.MinValue;
                else
                    newCells[i, j] = 0;

        return newCells;
    }
}
