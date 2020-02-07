﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using System.Linq;


[RequireComponent(typeof(BoxCollider))]
public class ScalarField : MonoBehaviour
{
    [Range(1, 10)]
    public int worldSize = 5;

    public Material material;

    [Header("Primary Noise")]
    [Range(0.001f, 0.2f)]
    public float noiseScale = 0.1f;
    [Range(5, 200)]
    public float noiseIntensity = 1f;

    [Header("Secondary Noise")]
    [Range(0, 75)]
    public float secondaryNoiseIntensity = 1f;
    [Range(0.01f, 0.2f)]
    public float secondaryNoiseScale = 0.1f;
    public Vector3 secondaryNoiseOffset;

    [Header("3D Noise")]
    [Range(0, 75)]
    public float noise3DIntensity = 1f;
    [Range(0.01f, 0.2f)]
    public float noise3DScale = 0.1f;

    [Header("Mask")]
    public MaskType maskType = MaskType.None;
    [Range(0, 0.05f)]
    public float maskScale = 0.01f;

    [Header("Visualization")]
    public bool visualize = false;
    public bool realTimeMesh = false;
    [Range(1, 8)]
    public int gridSize = 2;

    [Header("GPU")]
    public bool useGPU = false;
    public ComputeShader computeShader;
    [Range(0.01f, 5)]
    public float trianglePerCube = 5;

    [Header("Water")]
    public bool createWater = false;
    [Range(-25, 25)]
    public float waterLevel = 0;
    public GameObject waterPrefab;

    private static ScalarField instance;
    public enum MaskType { Linear, Sigmoid, Step, Beta, None };

    void Awake()
    {
        instance = this;
    }

    public static ScalarField Get()
    {
        return instance;
    }

    float GetPointValue(Vector3 p)
    {
        // return Mathf.Max(0, p.magnitude);
        return -p.y + Perlin.Noise(p.x * noiseScale, 0, p.z * noiseScale);
    }

    float GetMaskValue(Vector3 p)
    {
        if (maskType != MaskType.None)
        {
            // 2D Noise between 0-1
            float v = (Perlin.Noise(p.x * maskScale, 0, p.z * maskScale) + 1) / 2.0f;

            if (maskType == MaskType.Linear)
            {
                return v;
            }
            else if (maskType == MaskType.Sigmoid)
            {
                return 1.0f / (1.0f + Mathf.Exp(-v * 20 + 8));
            }
            else if (maskType == MaskType.Step)
            {
                return v > 0.5f ? 1 : 0;
            }
            else if (maskType == MaskType.Beta)
            {
                float beta = 3;
                return 1 / (1 + Mathf.Pow((v / (1 - v)), -beta));
            }
        }

        return 1;
    }

    public bool IsPointOutside(Vector3 p)
    {
        float mainNoise = Perlin.Noise(p.x * noiseScale, 0, p.z * noiseScale) * noiseIntensity;
        float secondaryNoise = Perlin.Noise(p.x * secondaryNoiseScale + secondaryNoiseOffset.x, secondaryNoiseOffset.y, p.z * secondaryNoiseScale + secondaryNoiseOffset.z) * secondaryNoiseIntensity;
        float noise3D = Perlin.Noise(p.x * noise3DScale, p.y * noise3DScale, p.z * noise3DScale) * noise3DIntensity;
        float maskFactor = GetMaskValue(p);
        return p.y < (mainNoise + secondaryNoise + noise3D) * maskFactor;
    }

    public void ClearTerrain()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    public void CreateWorld()
    {
        ClearTerrain();

        BoxCollider box = GetComponent<BoxCollider>();

        worldSize = Mathf.Min(worldSize, 10);
        for (int i = 0; i < worldSize; i++)
        {
            for (int j = 0; j < worldSize; j++)
            {
                if (useGPU)
                {
                    CreateChunkGPU_Optimized(new Vector3(box.size.x * i, 0, box.size.z * j));
                }
                else
                {
                    CreateChunk(new Vector3(box.size.x * i, 0, box.size.z * j));
                }
            }
        }

        if (createWater)
        {
            GameObject waterPlane = Instantiate(waterPrefab);
            waterPlane.transform.parent = transform;
            //new Vector3((box.size.x-1) * worldSize / 2, waterLevel, (box.size.z-1) * worldSize / 2);
            waterPlane.transform.localScale = new Vector3(box.size.x, 1, box.size.z) * worldSize / 10;
            Vector3 offset = new Vector3(-box.size.x / 2, 0, -box.size.z / 2) + worldSize / 2.0f * new Vector3(box.size.x, 0, box.size.z);
            waterPlane.transform.localPosition = offset;
        }
    }

    public GameObject CreateChunk(Vector3 offset)
    {
        BoxCollider box = GetComponent<BoxCollider>();
        Vector3 boxOrigin = transform.position + box.center;
        int cubeIndex = 0;

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        GameObject obj = new GameObject("Chunk");
        obj.transform.position = offset;
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
                    cubeConfig += !IsPointOutside(offset + worldPos + new Vector3(0, 0, 1)) ? 1 : 0;
                    cubeConfig += !IsPointOutside(offset + worldPos + new Vector3(1, 0, 1)) ? 2 : 0;
                    cubeConfig += !IsPointOutside(offset + worldPos + new Vector3(1, 0, 0)) ? 4 : 0;
                    cubeConfig += !IsPointOutside(offset + worldPos + new Vector3(0, 0, 0)) ? 8 : 0;
                    cubeConfig += !IsPointOutside(offset + worldPos + new Vector3(0, 1, 1)) ? 16 : 0;
                    cubeConfig += !IsPointOutside(offset + worldPos + new Vector3(1, 1, 1)) ? 32 : 0;
                    cubeConfig += !IsPointOutside(offset + worldPos + new Vector3(1, 1, 0)) ? 64 : 0;
                    cubeConfig += !IsPointOutside(offset + worldPos + new Vector3(0, 1, 0)) ? 128 : 0;

                    if (cubeConfig == 255 || cubeConfig == 0) continue;

                    verts.Add(new Vector3(0.5f, 0, 1) + worldPos); /* OR */ // worldpos + (vert0 + vert1) / 2 /* OR WITH INTERPOLATION */ worldpos + (vert0 * noise@vert0 + vert1 * noise@vert1) / (noise@v1 + noise@v2))
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
                    verts.Add(new Vector3(0, 0.5f, 0) + worldPos); // Reduce vertex count to optimize

                    for (int i = 0; i < 5; i++)
                    {
                        int edge1 = TriangulationTable.triangulation[cubeConfig, i * 3];
                        int edge2 = TriangulationTable.triangulation[cubeConfig, i * 3 + 1];
                        int edge3 = TriangulationTable.triangulation[cubeConfig, i * 3 + 2];
                        if (edge1 >= 0 && edge2 >= 0 && edge3 >= 0)
                        {
                            tris.Add(cubeIndex * 12 + edge1);
                            tris.Add(cubeIndex * 12 + edge2);
                            tris.Add(cubeIndex * 12 + edge3);
                        }
                    }

                    cubeIndex++;
                }
            }
        }

        // Debug.Log("Done generating mesh (CPU). Vertices: " + verts.Count + "  Triangles: " + tris.Count);
        obj.GetComponent<MeshGenerator>().GenerateMesh_CPU(verts.ToArray(), tris.ToArray(), material);
        return obj;
    }


    void OnDrawGizmos()
    {
        if (realTimeMesh)
        {
            ClearTerrain();
            CreateChunkGPU_Optimized(Vector3.zero);
        }
        else if (visualize)
        {
            BoxCollider box = GetComponent<BoxCollider>();
            Vector3 boxOrigin = transform.position + box.center;
            for (int x = 0; x <= box.size.x; x += gridSize)
            {
                for (int y = 0; y <= box.size.y; y += gridSize)
                {
                    for (int z = 0; z <= box.size.z; z += gridSize)
                    {
                        Vector3 worldPos = boxOrigin - box.size / 2 + new Vector3(x, y, z);

                        if (IsPointOutside(worldPos))
                        {
                            Debug.DrawLine(worldPos, worldPos + Vector3.up * 0.1f);
                        }
                    }
                }
            }
        }
    }

    public GameObject CreateChunkGPU_Optimized(Vector3 offset)
    {
        // TODO: WE BLOW GPU MEMORY!!! WE NEED TO REDUCE THE BUFFER SIZE. USE LOW PRECISION INTS (I16)?
        BoxCollider box = GetComponent<BoxCollider>();
        Vector3 boxOrigin = transform.position + box.center + offset;

        int cubeCount = (int)(box.size.x + 1) * (int)(box.size.y + 1) * (int)(box.size.z + 1);

        if (box.size.x >= 512 || box.size.y >= 512 || box.size.z >= 512)
        {
            Debug.LogError("Maximum thread count reached! Aborting...");
            return null;
        }

        int kernel = computeShader.FindKernel("CSMain");

        ComputeBuffer triBuffer = new ComputeBuffer((int)(cubeCount * trianglePerCube), sizeof(int) * 3 * 3, ComputeBufferType.Append);
        triBuffer.SetCounterValue(0);

        ComputeBuffer triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        computeShader.SetBuffer(kernel, "tris", triBuffer);
        computeShader.SetVector("boxSize", new Vector4(box.size.x + 1, box.size.y + 1, box.size.z + 1, 0));
        computeShader.SetVector("boxOrigin", new Vector4(boxOrigin.x, boxOrigin.y, boxOrigin.z, 0));
        computeShader.SetFloat("noiseScale", noiseScale);
        computeShader.SetFloat("noiseIntensity", noiseIntensity);
        computeShader.SetFloat("secondaryNoiseIntensity", secondaryNoiseIntensity);
        computeShader.SetFloat("secondaryNoiseScale", secondaryNoiseScale);
        computeShader.SetVector("secondaryNoiseOffset", secondaryNoiseOffset);
        computeShader.SetFloat("noise3DIntensity", noise3DIntensity);
        computeShader.SetFloat("noise3DScale", noise3DScale);
        computeShader.SetFloat("maskScale", maskScale);

        Triangle[] tris = new Triangle[(int)(cubeCount * trianglePerCube)];
        triBuffer.SetData(tris);

        const int numThreadsPerAxis = 8; // This comes from the Compute Shader
        Vector3Int numThreadGroups = new Vector3Int(Mathf.CeilToInt(box.size.x / numThreadsPerAxis), Mathf.CeilToInt(box.size.y / numThreadsPerAxis), Mathf.CeilToInt(box.size.z / numThreadsPerAxis));
        // Debug.Log("Num Thread Groups: " + numThreadGroups);

        computeShader.Dispatch(kernel, numThreadGroups.x, numThreadGroups.y, numThreadGroups.z);

        triBuffer.GetData(tris);
        ComputeBuffer.CopyCount(triBuffer, triCountBuffer, 0);
        int[] numTriangleVertices = new int[1];
        triCountBuffer.GetData(numTriangleVertices);

        int numTriangles = numTriangleVertices[0];

        int[] meshTris = new int[numTriangles * 3];
        Vector3[] meshVerts = new Vector3[numTriangles * 3];

        for (int i = 0; i < numTriangles; i++)
        {
            meshTris[i * 3] = i * 3;
            meshVerts[i * 3] = (Vector3)tris[i].v1 / 2;

            meshTris[i * 3 + 1] = i * 3 + 1;
            meshVerts[i * 3 + 1] = (Vector3)tris[i].v2 / 2;

            meshTris[i * 3 + 2] = i * 3 + 2;
            meshVerts[i * 3 + 2] = (Vector3)tris[i].v3 / 2;
        }

        // Generate Mesh
        GameObject obj = new GameObject("CSTest");
        obj.transform.position = Vector3.zero;
        obj.transform.parent = transform;
        obj.AddComponent<MeshGenerator>();
        obj.AddComponent<MeshFilter>();
        obj.AddComponent<MeshRenderer>();
        obj.GetComponent<MeshGenerator>().GenerateMesh_CPU(meshVerts, meshTris, material);

        // Debug.Log("Done generating mesh (GPU). Vertices: " + meshVerts.Length + "  Triangles: " + meshTris.Length);

        triCountBuffer.Release();
        triBuffer.Release();

        return obj;
    }
}

struct Triangle
{
    public Vector3Int v1;
    public Vector3Int v2;
    public Vector3Int v3;
};

