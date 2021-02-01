using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private GameObject playerPrefab;
    private TerrainGeneration terrain;
    private GameObject createdPlayer;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        terrain = TerrainGeneration.instance;
        playerPrefab = Resources.Load<GameObject>("Prefabs/Player/PlayerNew");

        var fromSave = PlayerPrefs.HasKey("LoadGame") && PlayerPrefs.GetInt("LoadGame") == 1;
        CreateGame(fromSave);        
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
