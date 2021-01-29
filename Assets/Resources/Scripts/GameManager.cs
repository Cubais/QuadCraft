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

    private void Start()
    {
        CreateGame(true);
    }
    public GameObject GetPlayer()
    {
        return createdPlayer;
    }

    public void CreateGame(bool fromSave)
    {
        if (fromSave)
        {
            var data = SaveManager.instance.LoadTerrainData();
            terrain.GenerateTerrain(true, data);
        }
        else
        {
            terrain.GenerateTerrain(false);
        }

        CreatePlayer(fromSave);
    }

    void CreatePlayer(bool fromSave)
    {
        Vector3 playerPosition;
        if (fromSave)
        {
            playerPosition = SaveManager.instance.LoadPlayerPosition();
        }
        else
        {
            playerPosition = TerrainGeneration.instance.GetCenterPosition();
        }

        createdPlayer = Instantiate(playerPrefab, playerPosition, Quaternion.identity);
        InputManager.instance.SetPlayer(createdPlayer.GetComponent<IPLayerInput>());
    }
}
