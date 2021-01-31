using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(instance.gameObject);
            instance = this;
        }
    }

    /// <summary>
    /// Saves game data
    /// </summary>
    public void SaveGame()
    {
        var terrainData = TerrainGeneration.instance.SaveTerrain();
        var chunks = TerrainGeneration.instance.SaveChunksIndividually();

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/gamesave.save");
        bf.Serialize(file, terrainData);
        file.Close();

        if (Directory.Exists(Application.persistentDataPath + "/ChunksData"))
        {
            Directory.Delete(Application.persistentDataPath + "/ChunksData", true);
        }

        Directory.CreateDirectory(Application.persistentDataPath + "/ChunksData");

        // Saves each chunk into separate file
        foreach (var chunk in chunks)
        {
            var sufix = chunk.worldPosition.x + "_" + chunk.worldPosition.y + "_ChunkData.save";
            file = File.Create(Application.persistentDataPath + "/ChunksData/" + sufix);
            bf.Serialize(file, chunk);
            file.Close();

            Debug.Log("SAVE: " + chunk.worldPosition.x + " " + chunk.worldPosition.y);
        }

        // Save player position
        var playerPosition = GameManager.instance.GetPlayer().transform.position;
        file = File.Create(Application.persistentDataPath + "/playerData.save");
        bf.Serialize(file, new SVector3(playerPosition));
        file.Close();
    }

    /// <summary>
    /// Loads terrain data
    /// </summary>
    /// <returns>Loaded TerrainSaveData</returns>
    public TerrainSaveData LoadTerrainData()
    {
        if (File.Exists(Application.persistentDataPath + "/gamesave.save"))
        {
            var bf = new BinaryFormatter();
            var file = File.Open(Application.persistentDataPath + "/gamesave.save", FileMode.Open);
            var terrainData = (TerrainSaveData)bf.Deserialize(file);
            file.Close();

            return terrainData;
        }

        return null;
    }

    /// <summary>
    /// Loads data of the individual chunk
    /// </summary>
    /// <param name="chunkWorldPosition">Wolrd position of the chunk</param>
    /// <returns>Filled TerrainCunkData</returns>
    public TerrainChunkData LoadChunkData(Vector2 chunkWorldPosition)
    {
        var path = Application.persistentDataPath + "/ChunksData/" + chunkWorldPosition.x + "_" + chunkWorldPosition.y + "_ChunkData.save";
        if (File.Exists(path))
        {
            var bf = new BinaryFormatter();
            var file = File.Open(path, FileMode.Open);
            var terrainChunkData = (TerrainChunkData)bf.Deserialize(file);
            file.Close();

            return terrainChunkData;
        }

        return null;
    }

    /// <summary>
    /// Loads player position
    /// </summary>
    /// <returns>Player position in the world</returns>
    public Vector3 LoadPlayerPosition()
    {
        var path = Application.persistentDataPath + "/playerData.save";
        if (File.Exists(path))
        {
            var bf = new BinaryFormatter();
            var file = File.Open(path, FileMode.Open);
            var sVector3 = (SVector3)bf.Deserialize(file);
            file.Close();

            return sVector3.GetVector3();
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Does the saved game exists
    /// </summary>
    /// <returns>rue if game exists</returns>
    public bool SaveGameExists()
    {
        return File.Exists(Application.persistentDataPath + "/gamesave.save") && Directory.Exists(Application.persistentDataPath + "/ChunksData");
    }
}
