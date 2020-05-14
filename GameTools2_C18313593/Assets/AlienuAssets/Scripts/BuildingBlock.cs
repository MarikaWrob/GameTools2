using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingBlock : MonoBehaviour
{
    private int _recursions = 0;
    [HideInInspector] public int Height;
    [HideInInspector] public Biru building;

    public void Build(int recursion, int heightLimit, Biru building, Transform parent)
    {
        _recursions = recursion;
        Height = heightLimit;
        transform.parent = parent;
        MeshFilter mf = GetComponent<MeshFilter>();
        Renderer r = GetComponent<Renderer>();

        if (!mf && building.topMeshes.Length <= 0 ||
            building.midMeshes.Length <= 0 ||
            building.bottomMeshes.Length <= 0 ||
            building.midMaterials.Length <= 0 ||
            building.topMaterials.Length <= 0 ||
            building.bottomMaterials.Length <= 0) return;

        int index = 0;
        if (_recursions >= Height)
        {
            index = Random.Range(0, building.topMeshes.Length);
            mf.mesh = building.topMeshes[index];
            if (r)
                r.material = building.topMaterials[index];
        }
        else if (_recursions == 0) {
            index = Random.Range(0, building.bottomMeshes.Length);
            mf.mesh = building.bottomMeshes[index];
            if (r)
                r.material = building.bottomMaterials[index];
        }
        else
        {
            index = Random.Range(0, building.midMeshes.Length);
            mf.mesh = building.midMeshes[index];
            if (r)
                r.material = building.midMaterials[index];
        }
        if (building.randomizeColors)
        {
            Color c = new Color(Random.value, Random.value, Random.value);
            if (r)
                r.material.color = c;
        }


        if (_recursions < Height)
        {
            GameObject go = Instantiate(gameObject, new Vector3(transform.position.x, transform.position.y + 1, transform.position.z), Quaternion.identity);
            BuildingBlock block = go.GetComponent<BuildingBlock>();
            if (block)
            {
                block.Build(_recursions + 1, Height, building, transform);
            }
        }
    }
}
