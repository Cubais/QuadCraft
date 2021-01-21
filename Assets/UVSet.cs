using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UVSet : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        var UVs = new Vector2[mesh.vertices.Length];
        var numL = 0.49f;
        var numU = 0.51f;

        // Front
        UVs[0] = new Vector2(0.0f, 0.0f);
        UVs[1] = new Vector2(numL, 0.0f);
        UVs[2] = new Vector2(0.0f, numL);
        UVs[3] = new Vector2(numL, numL);

        // Top
        UVs[4] = new Vector2(0, numU);
        UVs[5] = new Vector2(numL, numU);
        UVs[8] = new Vector2(0, 1);
        UVs[9] = new Vector2(numL, 1);

        // Back
        UVs[6] = new Vector2(0.0f, 0.0f);
        UVs[7] = new Vector2(numL, 0.0f);
        UVs[10] = new Vector2(0.0f, numL);
        UVs[11] = new Vector2(numL, numL);

        // Bottom
        UVs[12] = new Vector2(numU, 0);
        UVs[13] = new Vector2(1, 0);
        UVs[14] = new Vector2(numU, numL);
        UVs[15] = new Vector2(1, numL);

        // Left
        UVs[19] = new Vector2(0.0f, 0.0f);
        UVs[16] = new Vector2(numL, 0.0f);
        UVs[17] = new Vector2(0.0f, numL);
        UVs[18] = new Vector2(numL, numL);

        // Right        
        UVs[23] = new Vector2(0.0f, 0.0f);
        UVs[20] = new Vector2(numL, 0.0f);
        UVs[21] = new Vector2(0.0f, numL);
        UVs[22] = new Vector2(numL, numL);

        mesh.uv = UVs;
    }
}
