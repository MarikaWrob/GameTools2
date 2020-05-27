using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Machi : MonoBehaviour
{
    public static Vector2Int kPointNotFound = new Vector2Int(-1, -1);
    public const int kDepthMin = 0;
    public const int kDepthMax = 6;
    public const float kGridTileHalfSize = 0.5f;

    [HideInInspector] public bool[,] machiNoGurido;

    [Header("References")]
    public GameObject buildingPrefab;

    [Header("Buildings")]
    public Biru[] tatemono;

    [Header("Paths")]
    public Material pathMaterial;

    [Header("Generation Settings")]
    public int biruPointoNoTargetto;
    public int randomizationAttemptLimit;
    [Range(0, 6)] public int buildingGenerationDepth = 1;
    public bool randomizeDepthEachPass = true;
    public Vector2Int buildingHeightLimits = Vector2Int.one;

    [Header("Navigation Settings")]
    public bool generateNavMesh = true;
    public LayerMask groundLayerMask;
    public string groundLayer;

    [Header("Grid Settings")]
    public int gridWidth;
    public int gridHeight;
    public float worldHeightMin = 0;

    private int _randomizationAttempts;

    private void OnEnable()
    {
        GenerateCity();
    }

    private void GenerateCity() {
        if (!buildingPrefab) {
            Debug.LogError("Failed to generate city. The Building Prefab is missing!");
            return;
        }
        machiNoGurido = new bool[gridWidth, gridHeight];
        SpreadBuildings();
        //After buildings are spread, buildings can be spawned on each spot
        for (int x = 0; x < machiNoGurido.GetLength(0); x++) {
            for (int y = 0; y < machiNoGurido.GetLength(1); y++) {
                if (machiNoGurido[x, y])
                    SpawnBuilding(x, y);
            }
        }
        SpawnGround();
    }

    private void SpawnGround() {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "City Ground";
        ground.transform.localScale = new Vector3(gridWidth, 0.05f, gridHeight);
        ground.transform.position = new Vector3((gridWidth / 2) - kGridTileHalfSize, worldHeightMin, (gridHeight / 2) - kGridTileHalfSize);
        ground.transform.rotation = Quaternion.identity;
        ground.layer = LayerMask.NameToLayer(groundLayer);
        Renderer r = ground.GetComponent<Renderer>();
        if (r) {
            if (!pathMaterial)
                r.material.color = Color.gray;
            else
                r.material = pathMaterial;
        }
        if (generateNavMesh) {
            NavMeshSurface surface = ground.AddComponent<NavMeshSurface>();
            if (surface) {
                surface.layerMask = groundLayerMask;
                surface.BuildNavMesh();
            }
        }       
    }

    private void SpawnBuilding(int x, int y) {
        GameObject building = Instantiate(buildingPrefab, new Vector3(x, worldHeightMin, y), Quaternion.identity);
        if (generateNavMesh) {
            NavMeshObstacle o = building.AddComponent<NavMeshObstacle>();
            o.carving = true;
        }
        BuildingBlock bb = building.GetComponent<BuildingBlock>();
        if (bb)
            bb.Build(0, Random.Range(buildingHeightLimits.x, buildingHeightLimits.y), tatemono[Random.Range(0, tatemono.Length)], building.transform); //Build random building
        building.transform.parent = transform;
    }

    private bool CanPopulatePoint(int x, int y) {
        return IsPointOnGrid(x, y) && !IsPointOccupied(x, y);
    }

    private void SpreadBuildings() {
        int depth = buildingGenerationDepth;
        for (int i = 0; i < biruPointoNoTargetto; i++) {
            Vector2Int point = GetRandomPointOnGrid();
            if (point == kPointNotFound) continue;
            machiNoGurido[point.x, point.y] = true;

            if (randomizeDepthEachPass)
                depth = Random.Range(kDepthMin, kDepthMax);

            for (int dx = -depth; dx <= depth; dx++)
            {
                for (int dy = -depth; dy <= depth; dy++)
                {
                    if (IsPointOnGrid(point.x + dx, point.y + dy))
                        machiNoGurido[point.x + dx, point.y + dy] = true;
                }
            }
        }
    }

    private Vector2Int GetRandomPointOnGrid() {
        int x = Random.Range(0, gridWidth);
        int y = Random.Range(0, gridHeight);

        if (CanPopulatePoint(x, y))
        {
            _randomizationAttempts = 0;
            return new Vector2Int(x, y);
        }

        if (_randomizationAttempts < randomizationAttemptLimit)
        {
            _randomizationAttempts++;
            return GetRandomPointOnGrid();
        }
        else
        {
            _randomizationAttempts = 0;
            return kPointNotFound;
        }
    }

    private bool IsPointOnGrid(int x, int y) {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    private bool IsPointOccupied(int x, int y) {
        return machiNoGurido[x, y];
    }

    private void CombineIntoMesh(GameObject objectToCombine)
    {
        MeshFilter[] meshFilters = objectToCombine.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combineInstance = new CombineInstance[meshFilters.Length];
        for (int i = 0; i < combineInstance.Length; i++)
        {
            combineInstance[i].mesh = meshFilters[i].sharedMesh;
            combineInstance[i].transform = meshFilters[i].transform.localToWorldMatrix;
            Destroy(meshFilters[i].gameObject);
        }
        MeshFilter meshFilter = objectToCombine.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = objectToCombine.AddComponent<MeshRenderer>();
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.CombineMeshes(combineInstance);
        meshFilter.mesh.Optimize();
        objectToCombine.SetActive(true);

        objectToCombine.transform.localScale = Vector3.one;
        objectToCombine.transform.rotation = Quaternion.identity;
        objectToCombine.transform.position = Vector3.zero;
    }
}
