using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainGeneration : MonoBehaviour
{
    public static TerrainGeneration instance;

    public int chunkSize;
    
    public float noiseScale;

    public Vector2 noiseStartOffset;

    public int startChunkSize = 3;

    public RawImage noiseImage;
        
    [SerializeField]
    GameObject terrainChunkPrefab;

    TerrainChunk[,] terrainChunks;

    float time;

    private void Awake()
    {
        terrainChunks = new TerrainChunk[startChunkSize, startChunkSize];

        // TODO: Check for proper singleton implementation
        if (!instance)
        {
            instance = this;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {            
            GenerateTerrain();
        }
    }

    public void GenerateTerrain()
    {
        time = Time.realtimeSinceStartup;

        terrainChunks[0, 0] = Instantiate(terrainChunkPrefab, Vector3.zero, Quaternion.identity).GetComponent<TerrainChunk>();
        terrainChunks[0, 0].GenerateChunk(chunkSize, noiseScale, noiseStartOffset);

        for (int x = 0; x <= terrainChunks.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= terrainChunks.GetUpperBound(1); y++)
            {
                if (terrainChunks[x, y])
                    continue;

                terrainChunks[x, y] = Instantiate(terrainChunkPrefab, new Vector3(x * chunkSize, 0, y * chunkSize), Quaternion.identity).GetComponent<TerrainChunk>();
                terrainChunks[x, y].GenerateChunk(chunkSize, noiseScale, new Vector2(x * noiseScale + noiseStartOffset.x, y * noiseScale + noiseStartOffset.y));
            }
        }

        Debug.Log(Time.realtimeSinceStartup - time);
    }
}
