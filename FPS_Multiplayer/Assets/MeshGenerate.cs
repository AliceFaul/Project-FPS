using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    private Mesh mesh;
    Vector3[] vertices; // Array of vertices for the mesh
    int[] triangles; // Array of triangle indices for the mesh

    [Header("Size")]
    public int xSize = 20; // Width of the mesh
    public int zSize = 20; // Depth of the mesh

    [Header("Mountain Setting")]
    public float scale = 20f;
    public int octaves = 4;
    [Range(0, 1)] public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float heightMultiplier = 1.5f;
    public Vector2 offset;

    [Header("Random")]
    public int seed = 123;
    public bool randomizeSeed = false;
    public Vector2[] octaveOffsets;

    [Header("Tree")]
    public GameObject treePrefab;
    [Range(0, 1)] public float treeDensity = 0.1f; // Percentage of vertices that will have trees
    public float minTreeHeight = 1f; // Minimum height for trees to spawn
    public float maxTreeHeight = 8f; // Maximum height for trees to spawn
    public bool spawnTrees = true; // Toggle for spawning trees


    //private void Awake() {
    //    mesh = new Mesh();
    //    mesh.name = "Procedural Mesh";

    //    GenerateMap();
    //    CreateShape();
    //    SpawnTree();
    //    UpdateMesh();

    //    GetComponent<MeshFilter>().mesh = mesh; // Assign the generated mesh to the MeshFilter component
    //}

    //private void Start() { 
    //    UpdateCollision(); // Ensure the mesh collider is updated with the generated mesh
    //}

    [ContextMenu("Generate Map")]
    public void Generate()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Mesh";

        GenerateMap();
        CreateShape();
        SpawnTree();
        UpdateMesh();

        GetComponent<MeshFilter>().mesh = mesh; // Assign the generated mesh to the MeshFilter component

        UpdateCollision(); // Ensure the mesh collider is updated with the new mesh
    }

    private void GenerateMap()
    {
        if (randomizeSeed)
        {
            seed = Random.Range(-10000, 10000);
        }

        System.Random prng = new System.Random(seed);
        octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
    }

    private void CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                var y = CalculateNoiseHeight(x, z);
                vertices[i] = new Vector3(x, y, z);
                i++;
            }
        }

        triangles = new int[xSize * zSize * 6];
        var vert = 0;
        var tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;

                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    private float CalculateNoiseHeight(int x, int z)
    {
        if (scale <= 0) scale = 0.0001f; // Prevent division by zero

        var amplitude = 1f;
        var frequency = 1f;
        var noiseHeight = 0f;

        for (int i = 0; i < octaves; i++)
        {
            var sampleX = (x + octaveOffsets[i].x) / scale * frequency;
            var sampleZ = (z + octaveOffsets[i].y) / scale * frequency;

            var perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1; // Convert to range [-1, 1]
            noiseHeight += perlinValue * amplitude; // Accumulate noise height

            amplitude *= persistence; // Reduce amplitude for next octave
            frequency *= lacunarity; // Increase frequency for next octave
        }

        return noiseHeight * heightMultiplier; // Scale the final height
    }

    private void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); // Recalculate normals for proper lighting
        mesh.RecalculateBounds(); // Recalculate bounds for correct rendering and collision detection
        mesh.Optimize(); // Optimize the mesh for better performance
    }

    private void UpdateCollision()
    {
        var meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        meshCollider.sharedMesh = null; // Reset the shared mesh to ensure it updates correctly
        meshCollider.sharedMesh = mesh;

        Physics.SyncTransforms(); // Ensure the physics engine is updated with the new mesh data
    }

    private void SpawnTree()
    {
        if (treePrefab == null)
        {
            Debug.LogWarning("Missing tree prefab reference");
            return;
        }

        if (!spawnTrees)
        {
            Debug.Log("Tree spawning is disabled");
            return;
        }

        // Khởi tạo lại bộ sinh số ngẫu nhiên theo Seed để cây mọc cố định theo Map
        System.Random prng = new System.Random(seed);

        // Duyệt qua từng đỉnh của Mesh
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexPos = vertices[i];

            // Kiểm tra điều kiện độ cao xem vị trí này có hợp lý để mọc cây không
            if (vertexPos.y >= minTreeHeight && vertexPos.y <= maxTreeHeight)
            {

                // Sinh một số ngẫu nhiên từ 0.0 đến 1.0
                float randomValue = (float)prng.NextDouble();

                // Nếu số ngẫu nhiên nhỏ hơn mật độ quy định -> Trồng cây!
                if (randomValue < treeDensity)
                {
                    Vector3 worldPos = transform.TransformPoint(vertexPos); // Chuyển từ local space sang world space
                    Instantiate(treePrefab, worldPos, Quaternion.identity, transform); // Tạo cây con ở vị trí đó
                }
            }
        }
    }
}