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

    public int numOfChunks = 3;

    public RawImage noiseImage;
        
    [SerializeField]
    GameObject terrainChunkPrefab;

    TerrainChunk[,] terrainChunks;

    float time;
    private void Awake()
    {
        terrainChunks = new TerrainChunk[numOfChunks, numOfChunks];

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

        if (Input.GetKeyDown(KeyCode.I))
        {
            CreateChunkSpace(ref terrainChunks, Direction.UP);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            CreateChunkSpace(ref terrainChunks, Direction.DOWN);
        }
    }

    /// <summary>
    /// Shifts map of chunks and load new ones in the given direction, unload old ones
    /// </summary>
    /// <param name="array">Array of chunks</param>
    /// <param name="direction">Directions where to load new chunks</param>
    void CreateChunkSpace(ref TerrainChunk[,] array, Direction direction)
    {
        switch (direction)
        {
            // Shifts array down and create new chunks at the top
            case Direction.UP:

                // GO throught chunks from bottom to the top
                for (int y = 0; y <= array.GetUpperBound(1); y++)
                {
                    for (int x = 0; x <= array.GetUpperBound(0); x++)
                    {
                        // Unload bottom chunks
                        if (y == 0)
                        {
                            // Unload these
                            array[x, y].DisableChunk();
                            array[x, y] = array[x, y + 1];
                        }
                        // Generate new chunks at the top or load if they were generated
                        else if (y == array.GetUpperBound(1))
                        {
                            if (array[x, y - 1].GetNeighbour(Direction.UP) == null)
                            {
                                var chunkUnderPosition = array[x, y - 1].transform.position;
                                var noiseOffset = array[x, y - 1].noiseOffset;

                                array[x, y] = Instantiate(terrainChunkPrefab, new Vector3(chunkUnderPosition.x, 0, chunkUnderPosition.z + chunkSize), Quaternion.identity).GetComponent<TerrainChunk>();
                                array[x, y].GenerateChunk(chunkSize, noiseScale, new Vector2(noiseOffset.x, noiseOffset.y + noiseScale));
                            }
                            else
                            {
                                array[x, y] = array[x, y - 1].GetNeighbour(Direction.UP);
                                array[x, y].LoadChunk();
                            }
                        }
                        else
                        {
                            array[x, y] = array[x, y + 1];
                        }
                    }
                }

                break;


            case Direction.LEFT:

                // Go throught chunks from top to bottom
                for (int x = 0; x <= array.GetUpperBound(0); x++)
                {
                    for (int y = 0; y <= array.GetUpperBound(1); y++)
                    {
                        // Generate new chunks at the bottom
                        if (x == 0)
                        {
                            // Unload these
                            //array[x, y].UNLOAD();
                            array[x, y] = array[x + 1, y];
                        }
                        // Unload chunks at the top
                        else if (x == array.GetUpperBound(0))
                        {
                            //array[x, y].GENERATE();
                            array[x, y] = null;
                        }
                        else
                        {
                            array[x, y] = array[x + 1, y];
                        }
                    }
                }

                break;
            case Direction.DOWN:

                // Go throught chunks from top to bottom
                for (int y = array.GetUpperBound(1); y >= 0; y--)
                {
                    for (int x = 0; x <= array.GetUpperBound(0); x++)
                    {
                        // Generate new chunks at the bottom
                        if (y == 0)
                        {
                            if (array[x, y + 1].GetNeighbour(Direction.DOWN) == null)
                            {
                                var chunkAbovePosition = array[x, y + 1].transform.position;
                                var noiseOffset = array[x, y + 1].noiseOffset;

                                array[x, y] = Instantiate(terrainChunkPrefab, new Vector3(chunkAbovePosition.x, 0, chunkAbovePosition.z - chunkSize), Quaternion.identity).GetComponent<TerrainChunk>();
                                array[x, y].GenerateChunk(chunkSize, noiseScale, new Vector2(noiseOffset.x, noiseOffset.y - noiseScale));
                            }
                            else
                            {
                                array[x, y] = array[x, y + 1].GetNeighbour(Direction.DOWN);
                                array[x, y].LoadChunk();
                            }
                        }
                        // Unload chunks at the top
                        else if (y == array.GetUpperBound(1))
                        {
                            // Unload these
                            array[x, y].DisableChunk();
                            array[x, y] = array[x, y - 1];
                        }
                        else
                        {
                            array[x, y] = array[x, y - 1];
                        }
                    }
                }
                break;
            case Direction.RIGHT:

                for (int x = array.GetUpperBound(0); x >= 0; x--)
                {
                    for (int y = 0; y <= array.GetUpperBound(1); y++)
                    {
                        // Generate new chunks at the bottom
                        if (x == 0)
                        {
                            //array[x, y].GENERATE();
                            array[x, y] = null;
                        }
                        // Unload chunks at the top
                        else if (x == array.GetUpperBound(0))
                        {
                            // Unload these
                            //array[x, y].UNLOAD();
                            array[x, y] = array[x - 1, y];
                        }
                        else
                        {
                            array[x, y] = array[x - 1, y];
                        }
                    }
                }
                break;
            default:
                break;
        }

        SetChunkNeighbours(terrainChunks);
    }

    void GenerateTerrain()
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

        SetChunkNeighbours(terrainChunks);

        Debug.Log(Time.realtimeSinceStartup - time);
    }

    void SetChunkNeighbours(TerrainChunk[,] chunks)
    {
        for (int x = 0; x <= chunks.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= chunks.GetUpperBound(1); y++)
            {
                // Left-most side
                if (x == 0)
                {
                    chunks[x, y].SetNeighbour(Direction.RIGHT, chunks[x + 1, y]);
                }
                // Righ-most side
                else if (x == chunks.GetUpperBound(0))
                {
                    chunks[x, y].SetNeighbour(Direction.LEFT, chunks[x - 1, y]);
                }
                else
                {
                    chunks[x, y].SetNeighbour(Direction.RIGHT, chunks[x + 1, y]);
                    chunks[x, y].SetNeighbour(Direction.LEFT , chunks[x - 1, y]);
                }
            }
        }

        for (int y = 0; y <= chunks.GetUpperBound(1); y++)
        {
            for (int x = 0; x <= chunks.GetUpperBound(0); x++)
            {
                // Bottom side
                if (y == 0)
                {
                    chunks[x, y].SetNeighbour(Direction.UP, chunks[x, y + 1]);
                }
                // Top side
                else if (y == chunks.GetUpperBound(1))
                {
                    chunks[x, y].SetNeighbour(Direction.DOWN, chunks[x, y - 1]);
                }
                else
                {
                    chunks[x, y].SetNeighbour(Direction.UP, chunks[x, y + 1]);
                    chunks[x, y].SetNeighbour(Direction.DOWN, chunks[x, y - 1]);
                }
            }
        }
    }
}
