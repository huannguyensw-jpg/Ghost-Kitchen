using UnityEditor;
using UnityEngine;

public sealed class RestaurantColliderPostprocessor : AssetPostprocessor
{
    private const string RestaurantModelPath =
        "Assets/BuiCaoTanAn/Restaurantv3.fbx";

    // Giữ lại sàn, vách, quầy và bàn lớn; bỏ các chi tiết trang trí nhỏ.
    private const float MinimumMainAxisSize = 1.5f;
    private const float MinimumSecondAxisSize = 0.6f;

    private void OnPostprocessModel(GameObject root)
    {
        if (assetPath != RestaurantModelPath)
        {
            return;
        }

        foreach (MeshFilter filter in
                 root.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null || !NeedsCollision(filter, mesh))
            {
                continue;
            }

            BoxCollider collider =
                filter.gameObject.AddComponent<BoxCollider>();
            collider.center = mesh.bounds.center;
            collider.size = mesh.bounds.size;
        }
    }

    private static bool NeedsCollision(MeshFilter filter, Mesh mesh)
    {
        Vector3 scale = filter.transform.lossyScale;
        scale = new Vector3(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z)
        );
        Vector3 size = Vector3.Scale(mesh.bounds.size, scale);

        float largest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        float smallest = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        float secondLargest = size.x + size.y + size.z - largest - smallest;

        return largest >= MinimumMainAxisSize &&
               secondLargest >= MinimumSecondAxisSize;
    }
}
