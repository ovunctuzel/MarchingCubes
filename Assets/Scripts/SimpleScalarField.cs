using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SimpleScalarFieldEditor : MonoBehaviour
{
    [CustomEditor(typeof(SimpleScalarField))]
    public class TilemapPatcherEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        
            SimpleScalarField myScript = (SimpleScalarField)target;
            if(GUILayout.Button("Generate Mesh"))
            {
                myScript.CreateChunk();
            }
        }
    }
}

public class SimpleScalarField : MonoBehaviour
{
    public bool visualize = false;

    [Range(0.0f, 10.0f)]
    public float threshold = 0.5f;
    public Material material;


    float GetScalarFieldValue(Vector3 p)
    {
        return p.magnitude;
    }

    bool IsPointOutside(Vector3 p)
    {
        return GetScalarFieldValue(p) < threshold;
    }

    void OnDrawGizmos() 
    {
        if(visualize)
        {
            BoxCollider box = GetComponent<BoxCollider>();
            Vector3 boxOrigin = transform.position + box.center;
            for (int x = 0; x <= box.size.x; x ++)
            {
                for (int y = 0; y <= box.size.y; y ++)
                {
                    for (int z = 0; z <= box.size.z; z ++)
                    {
                        Vector3 worldPos = boxOrigin - box.size / 2 + new Vector3(x, y, z);

                        if(IsPointOutside(worldPos))
                        {
                            Debug.DrawLine(worldPos, worldPos + Vector3.up * 0.1f);
                        }
                    }
                }
            }
        }
    }

    public void CreateChunk()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        BoxCollider box = GetComponent<BoxCollider>();
        Vector3 boxOrigin = transform.position + box.center;
        int cubeIndex = 0;

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        GameObject obj = new GameObject("Chunk");
        obj.transform.position = Vector3.zero;
        obj.transform.parent = transform;
        obj.AddComponent<MeshGenerator>();
        obj.AddComponent<MeshFilter>();
        obj.AddComponent<MeshRenderer>();

        for (int x = 0; x <= box.size.x; x++)
        {
            for (int y = 0; y <= box.size.y; y++)
            {
                for (int z = 0; z <= box.size.z; z++)
                {
                    Vector3 worldPos = boxOrigin - box.size / 2 + new Vector3(x, y, z);

                    int cubeConfig = 0;
                    cubeConfig += !IsPointOutside(worldPos + new Vector3(0, 0, 1)) ? 1 : 0;
                    cubeConfig += !IsPointOutside(worldPos + new Vector3(1, 0, 1)) ? 2 : 0;
                    cubeConfig += !IsPointOutside(worldPos + new Vector3(1, 0, 0)) ? 4 : 0;
                    cubeConfig += !IsPointOutside(worldPos + new Vector3(0, 0, 0)) ? 8 : 0;
                    cubeConfig += !IsPointOutside(worldPos + new Vector3(0, 1, 1)) ? 16 : 0;
                    cubeConfig += !IsPointOutside(worldPos + new Vector3(1, 1, 1)) ? 32 : 0;
                    cubeConfig += !IsPointOutside(worldPos + new Vector3(1, 1, 0)) ? 64 : 0;
                    cubeConfig += !IsPointOutside(worldPos + new Vector3(0, 1, 0)) ? 128 : 0;

                    if(cubeConfig == 255 || cubeConfig == 0) continue;

                    verts.Add(new Vector3(0.5f, 0, 1) + worldPos);
                    verts.Add(new Vector3(1, 0, 0.5f) + worldPos);
                    verts.Add(new Vector3(0.5f, 0, 0) + worldPos);
                    verts.Add(new Vector3(0, 0, 0.5f) + worldPos);
                    verts.Add(new Vector3(0.5f, 1, 1) + worldPos);
                    verts.Add(new Vector3(1, 1, 0.5f) + worldPos);
                    verts.Add(new Vector3(0.5f, 1, 0) + worldPos);
                    verts.Add(new Vector3(0, 1, 0.5f) + worldPos);
                    verts.Add(new Vector3(0, 0.5f, 1) + worldPos);
                    verts.Add(new Vector3(1, 0.5f, 1) + worldPos);
                    verts.Add(new Vector3(1, 0.5f, 0) + worldPos);
                    verts.Add(new Vector3(0, 0.5f, 0) + worldPos);

                    for(int i = 0; i < 5; i++)
                    {   
                        int edge1 = TriangulationTable.triangulation[cubeConfig, i*3];
                        int edge2 = TriangulationTable.triangulation[cubeConfig, i*3+1];
                        int edge3 = TriangulationTable.triangulation[cubeConfig, i*3+2];
                        if(edge1 >= 0 && edge2 >= 0 && edge3 >= 0)
                        {
                            tris.Add(cubeIndex*12 + edge1);
                            tris.Add(cubeIndex*12 + edge2);
                            tris.Add(cubeIndex*12 + edge3);
                        }
                    }

                    cubeIndex++;
                }
            }
        }

        Debug.Log("Done generating mesh (CPU). Vertices: " + verts.Count + "  Triangles: " + tris.Count);
        obj.GetComponent<MeshGenerator>().GenerateMesh_CPU(verts.ToArray(), tris.ToArray(), material);
    }
}
