using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class IslandGenerator : MonoBehaviour
{
    [Header("Shape")]
    [Range(0f, 1f)] public float shapeBlend = 0f;
    public float radius = 20f;
    public int resolution = 128;

    [Header("Top Surface")]
    public float noiseAmount = 0.3f;

    [Header("Edge")]
    public float edgeWidth = 2f;
    public float edgeDrop = 1f;

    [Header("Bottom")]
    public float depth = 10f;
    public float inwardCurve = 3f;

    private Mesh mesh;
    private MeshCollider meshCollider;

    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        Generate();
    }

    void Generate()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        GetComponent<MeshFilter>().mesh = mesh;

        int rings = 3;
        int ringVertices = resolution;
        int vertexCount = rings * ringVertices + 2;

        Vector3[] vertices = new Vector3[vertexCount];

        mesh.subMeshCount = 4;

        int[][] submeshTriangles = new int[4][];
        submeshTriangles[0] = new int[resolution * 3];
        submeshTriangles[1] = new int[resolution * 6];
        submeshTriangles[2] = new int[resolution * 6];
        submeshTriangles[3] = new int[resolution * 3];

        int[] triIndex = new int[4];

        int v = 0;

        for (int ring = 0; ring < rings; ring++)
        {
            float height = 0f;
            float r = radius;

            if (ring == 1)
            {
                height = -edgeDrop;
                r = radius + edgeWidth;
            }
            else if (ring == 2)
            {
                height = -depth;
                r = radius - inwardCurve;
            }

            for (int i = 0; i < resolution; i++)
            {
                float angle = (float)i / resolution * Mathf.PI * 2f;

                float x = Mathf.Cos(angle);
                float y = Mathf.Sin(angle);

                float blend = shapeBlend;

                float sx = x / Mathf.Sqrt(1 - blend * y * y);
                float sy = y / Mathf.Sqrt(1 - blend * x * x);

                float px = sx * r;
                float py = sy * r;

                float noise = (ring == 0) ? Mathf.PerlinNoise(px * 0.1f, py * 0.1f) * noiseAmount : 0f;

                vertices[v] = new Vector3(px, height + noise, py);
                v++;
            }
        }

        int topCenter = v++;
        int bottomCenter = v++;

        vertices[topCenter] = new Vector3(0, 0, 0);
        vertices[bottomCenter] = new Vector3(0, -depth, 0);

        // TOP CAP (normals up)
        for (int i = 0; i < resolution; i++)
        {
            int next = (i + 1) % resolution;

            submeshTriangles[0][triIndex[0]++] = topCenter;
            submeshTriangles[0][triIndex[0]++] = next;
            submeshTriangles[0][triIndex[0]++] = i;
        }

        // EDGE
        for (int i = 0; i < resolution; i++)
        {
            int next = (i + 1) % resolution;

            int top = i;
            int edge = resolution + i;
            int edgeNext = resolution + next;
            int topNext = next;

            submeshTriangles[1][triIndex[1]++] = top;
            submeshTriangles[1][triIndex[1]++] = edgeNext;
            submeshTriangles[1][triIndex[1]++] = edge;

            submeshTriangles[1][triIndex[1]++] = top;
            submeshTriangles[1][triIndex[1]++] = topNext;
            submeshTriangles[1][triIndex[1]++] = edgeNext;
        }

        // SIDE
        for (int i = 0; i < resolution; i++)
        {
            int next = (i + 1) % resolution;

            int edge = resolution + i;
            int bottom = resolution * 2 + i;
            int bottomNext = resolution * 2 + next;
            int edgeNext = resolution + next;

            submeshTriangles[2][triIndex[2]++] = edge;
            submeshTriangles[2][triIndex[2]++] = bottomNext;
            submeshTriangles[2][triIndex[2]++] = bottom;

            submeshTriangles[2][triIndex[2]++] = edge;
            submeshTriangles[2][triIndex[2]++] = edgeNext;
            submeshTriangles[2][triIndex[2]++] = bottomNext;
        }

        // BOTTOM CAP (normals down)
        int bottomStart = resolution * 2;

        for (int i = 0; i < resolution; i++)
        {
            int next = (i + 1) % resolution;

            submeshTriangles[3][triIndex[3]++] = bottomCenter;
            submeshTriangles[3][triIndex[3]++] = bottomStart + i;
            submeshTriangles[3][triIndex[3]++] = bottomStart + next;
        }

        mesh.vertices = vertices;

        mesh.SetTriangles(submeshTriangles[0], 0);
        mesh.SetTriangles(submeshTriangles[1], 1);
        mesh.SetTriangles(submeshTriangles[2], 2);
        mesh.SetTriangles(submeshTriangles[3], 3);

        mesh.RecalculateNormals();

        // APPLY COLLIDER
        meshCollider.sharedMesh = null;   // force refresh
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;      // island should NOT be convex
    }
}
