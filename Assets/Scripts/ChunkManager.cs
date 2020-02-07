using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public ScalarField scalarField;
    public Vector3Int chunkSize;
    public GameObject playerObj;
    
    [Range(0.1f, 5f)]
    public float tickPeriod = 1;
    [Range(1, 3)]
    public int neighborRadius = 1;
    public bool async = false;
    public bool generateMeshes = false;

    private List<Vector2Int> activeChunks = new List<Vector2Int>();
    private Vector2Int playerChunk;
    private Vector3 origin;


    void Start()
    {
        InvokeRepeating("Tick", 0, 1f);
        origin = -new Vector3(chunkSize.x / 2, 0, chunkSize.z / 2);
    }

    void Tick()
    {
        playerChunk = new Vector2Int(Mathf.FloorToInt((playerObj.transform.position.x - origin.x) / chunkSize.x), Mathf.FloorToInt((playerObj.transform.position.z - origin.z) / chunkSize.z));
        Debug.Log(playerChunk);

        if(async)
        {
            StartCoroutine(CreateNeighborChunksAsync());
        }
        else
        {
            CreateNeighborChunks();
        }
    }

    bool ChunkExists(Vector2Int point)
    {
        return activeChunks.Contains(point);
    }

    IEnumerator CreateNeighborChunksAsync()
    {
        for (int i = -neighborRadius; i <= neighborRadius; i++)
        {
            for (int j = -neighborRadius; j <= neighborRadius; j++)
            {
                Vector2Int chunkPos = playerChunk + new Vector2Int(i, j);
                if(!ChunkExists(chunkPos))
                {
                    GameObject chunk = scalarField.CreateChunkGPU_Optimized(new Vector3(chunkPos.x * chunkSize.x, 0, chunkPos.y * chunkSize.z));
                    activeChunks.Add(chunkPos);
                    if(generateMeshes)
                    {
                        chunk.AddComponent<MeshCollider>();
                        chunk.AddComponent<Rigidbody>().isKinematic = true;
                    }
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
    }

    void CreateNeighborChunks()
    {
        for (int i = -neighborRadius; i <= neighborRadius; i++)
        {
            for (int j = -neighborRadius; j <= neighborRadius; j++)
            {
                Vector2Int chunkPos = playerChunk + new Vector2Int(i, j);
                if(!ChunkExists(chunkPos))
                {
                    GameObject chunk = scalarField.CreateChunkGPU_Optimized(new Vector3(chunkPos.x * chunkSize.x, 0, chunkPos.y * chunkSize.z));
                    activeChunks.Add(chunkPos);
                    if(generateMeshes)
                    {
                        chunk.AddComponent<MeshCollider>();
                        chunk.AddComponent<Rigidbody>().isKinematic = true;
                    }
                }
            }
        }
    }
}
