using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Biru", menuName = "Biru")]
public class Biru : ScriptableObject
{
    [SerializeField] public Mesh[] topMeshes;
    [SerializeField] public Mesh[] midMeshes;
    [SerializeField] public Mesh[] bottomMeshes;
    [SerializeField] public Material[] topMaterials;
    [SerializeField] public Material[] midMaterials;
    [SerializeField] public Material[] bottomMaterials;
    public bool randomizeColors;
}
