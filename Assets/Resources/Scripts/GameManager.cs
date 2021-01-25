using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public GameObject playerPrefab;
    public TerrainGeneration terrain;

    private GameObject createdPlayer;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }        
    }

    void Start()
    {        
        var playerPosition = (terrain.numOfChunks) / 2f * terrain.chunkSize;
        terrain.GenerateTerrain();

        createdPlayer = Instantiate(playerPrefab, new Vector3(playerPosition, 30, playerPosition), Quaternion.identity);
        InputManager.instance.SetPlayer(createdPlayer.GetComponent<IPLayerInput>());
    }

    public GameObject GetPlayer()
    {
        return createdPlayer;
    }
}
