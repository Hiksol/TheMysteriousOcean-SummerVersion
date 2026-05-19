using UnityEngine;
using Mirror;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class NetworkPerlinWater : NetworkBehaviour
{
    [Header("Mesh Settings")]
    public int meshResolution = 50;
    public float meshSize = 10f;

    [Header("Wave Settings")]
    [SyncVar] public float perlinScale = 1f;
    [SyncVar] public float waveSpeed = 0.5f;
    [SyncVar] public float waveHeight = 0.5f;
    [SyncVar] public float offset = 0f;

    // Время старта генерации, синхронизируется с сервера
    [SyncVar] private double generationStartTime;

    private Mesh dynamicMesh;
    private Vector3[] baseVertices;

    public override void OnStartServer()
    {
        // фиксируем момент старта на сервере
        generationStartTime = NetworkTime.time;
    }

    void Start()
    {
        CreateCustomMesh();
    }

    void Update()
    {
        CalcNoise();
    }

    void CreateCustomMesh()
    {
        dynamicMesh = new Mesh
        {
            name = "Dynamic Water Mesh"
        };

        GetComponent<MeshFilter>().mesh = dynamicMesh;

        int vertexCount = (meshResolution + 1) * (meshResolution + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        int[] triangles = new int[meshResolution * meshResolution * 6];

        float step = meshSize / meshResolution;
        int vertexIndex = 0;

        for (int z = 0; z <= meshResolution; z++)
        {
            for (int x = 0; x <= meshResolution; x++)
            {
                float xPos = (x * step) - (meshSize * 0.5f);
                float zPos = (z * step) - (meshSize * 0.5f);

                vertices[vertexIndex] = new Vector3(xPos, 0, zPos);
                normals[vertexIndex] = Vector3.up;
                uv[vertexIndex] = new Vector2((float)x / meshResolution, (float)z / meshResolution);
                vertexIndex++;
            }
        }

        int triangleIndex = 0;
        for (int z = 0; z < meshResolution; z++)
        {
            for (int x = 0; x < meshResolution; x++)
            {
                int bottomLeft = z * (meshResolution + 1) + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * (meshResolution + 1) + x;
                int topRight = topLeft + 1;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;

                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }

        dynamicMesh.vertices = vertices;
        dynamicMesh.normals = normals;
        dynamicMesh.uv = uv;
        dynamicMesh.triangles = triangles;
        dynamicMesh.RecalculateBounds();

        baseVertices = (Vector3[])vertices.Clone();

        GetComponent<MeshCollider>().sharedMesh = dynamicMesh;
    }

    void CalcNoise()
    {
        if (dynamicMesh == null || baseVertices == null) return;

        Vector3[] vertices = (Vector3[])baseVertices.Clone();

        // вычисляем общее время с учётом старта на сервере
        double elapsed = NetworkTime.time - generationStartTime;
        float time = (float)elapsed * waveSpeed;

        for (int i = 0; i < vertices.Length; i++)
        {
            float pX = (vertices[i].x * perlinScale) + time + offset;
            float pZ = (vertices[i].z * perlinScale) + time + offset;
            vertices[i].y = Mathf.PerlinNoise(pX, pZ) * waveHeight;
        }

        dynamicMesh.vertices = vertices;
        dynamicMesh.RecalculateNormals();

        GetComponent<MeshCollider>().sharedMesh = dynamicMesh;
    }

    public void UpdateMeshResolution()
    {
        CreateCustomMesh();
    }
}
