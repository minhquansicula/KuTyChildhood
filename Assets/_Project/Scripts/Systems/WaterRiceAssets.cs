using UnityEngine;

/// <summary>Delivered vessels, reference contents and an isolated copy of the existing kitchen pot.</summary>
[CreateAssetMenu(menuName = "KuTy/Water and rice assets")]
public sealed class WaterRiceAssets : ScriptableObject
{
    public GameObject ladle, cup, bucket, basket, ladleWater, cupRice, bucketWater, basketRice, receiver;
    public Material waterMaterial, riceMaterial;
    public Vector3 receiverFloor;
    public float receiverRadius, receiverHeight;
    public bool IsReady => ladle && cup && bucket && basket && ladleWater && cupRice && bucketWater && basketRice &&
        receiver && waterMaterial && riceMaterial && receiverRadius > 0 && receiverHeight > 0;
}
