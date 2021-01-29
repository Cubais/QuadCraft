using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainSaveData
{
    public int chunkSize;
    public float noiseScale;
    public SVector2 startNoiseOffset;
    public int numOfChunks;
    public List<SVector2> terrainChunks;
}

[System.Serializable]
public class SVector2
{
    public float x;
    public float y;

    public SVector2() { }

    public SVector2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public SVector2(Vector2 vec)
    {
        x = vec.x;
        y = vec.y;
    }

    public Vector3 GetVector2()
    {
        return new Vector2(x, y);
    }
}

[System.Serializable]
public class SVector3
{
    public float x;
    public float y;
    public float z;

    public SVector3() { }
    public SVector3(Vector3 vec)
    {
        x = vec.x;
        y = vec.y;
        z = vec.z;
    }

    public Vector3 GetVector3()
    {
        return new Vector3(x, y, z);
    }
}

[System.Serializable]
public class TerrainChunkData
{
    public SVector2 worldPosition;
    public int chunkHeight;
    public SVector2 noiseOffset;
    public bool changedTerrain;
    public byte[] terrainChunkTexture;
    public List<BlockSaveData> groundBlocks;
    public List<BlockSaveData> invisibleBlocks;
}

[System.Serializable]
public class BlockSaveData
{
    public SVector3 position;
    public int type;

    public BlockSaveData(Vector3 position, BlockType type)
    {
        this.position = new SVector3(position);
        this.type = (int)type;
    }
}
