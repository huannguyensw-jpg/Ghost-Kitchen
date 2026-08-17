using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class RestaurantV3MeshColliderRepair
{
    private const string ScenePath =
        "Assets/minhnhat/Scenes/Introductionnhat.unity";

    private static bool repairQueued;

    static RestaurantV3MeshColliderRepair()
    {
        QueueRepair();
    }

    private static void QueueRepair()
    {
        if (repairQueued)
        {
            return;
        }

        repairQueued = true;
        EditorApplication.delayCall += TryRepair;
    }

    private static void TryRepair()
    {
        repairQueued = false;

        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            QueueRepair();
            return;
        }

        Scene scene = GetLoadedIntroScene();
        bool openedAdditively = !scene.IsValid();

        if (openedAdditively)
        {
            scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive
            );
        }

        GameObject restaurant = FindRestaurant(scene);

        if (restaurant == null)
        {
            Debug.LogWarning(
                "[RESTAURANT COLLIDER] Không tìm thấy Restaurantv3 trong Intro."
            );
            CloseIfNeeded(scene, openedAdditively);
            return;
        }

        MeshFilter[] meshFilters =
            restaurant.GetComponentsInChildren<MeshFilter>(true);
        BoxCollider[] boxColliders =
            restaurant.GetComponentsInChildren<BoxCollider>(true);
        MeshCollider[] oldMeshColliders =
            restaurant.GetComponentsInChildren<MeshCollider>(true);

        int validMeshCount = 0;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (IsUsableMesh(meshFilter.sharedMesh))
            {
                validMeshCount++;
            }
        }

        if (boxColliders.Length == 0 &&
            oldMeshColliders.Length == validMeshCount)
        {
            CloseIfNeeded(scene, openedAdditively);
            return;
        }

        foreach (BoxCollider boxCollider in boxColliders)
        {
            Object.DestroyImmediate(boxCollider, true);
        }

        foreach (MeshCollider meshCollider in oldMeshColliders)
        {
            Object.DestroyImmediate(meshCollider, true);
        }

        int createdCount = 0;
        int skippedCount = 0;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;

            if (!IsUsableMesh(mesh))
            {
                skippedCount++;
                continue;
            }

            MeshCollider meshCollider =
                meshFilter.gameObject.AddComponent<MeshCollider>();

            meshCollider.sharedMesh = mesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
            meshCollider.cookingOptions =
                MeshColliderCookingOptions.CookForFasterSimulation |
                MeshColliderCookingOptions.EnableMeshCleaning |
                MeshColliderCookingOptions.WeldColocatedVertices |
                MeshColliderCookingOptions.UseFastMidphase;

            createdCount++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            $"[RESTAURANT COLLIDER] Intro: đã xóa " +
            $"{boxColliders.Length} BoxCollider, tạo " +
            $"{createdCount} MeshCollider, bỏ qua " +
            $"{skippedCount} mesh rỗng/lỗi."
        );

        CloseIfNeeded(scene, openedAdditively);
    }

    private static bool IsUsableMesh(Mesh mesh)
    {
        if (mesh == null ||
            mesh.vertexCount < 3 ||
            mesh.subMeshCount == 0)
        {
            return false;
        }

        Bounds bounds = mesh.bounds;
        Vector3 size = bounds.size;

        return IsFinite(size.x) &&
               IsFinite(size.y) &&
               IsFinite(size.z) &&
               size.sqrMagnitude > 0.00000001f;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static Scene GetLoadedIntroScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.path == ScenePath)
            {
                return scene;
            }
        }

        return default;
    }

    private static GameObject FindRestaurant(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);

            foreach (Transform candidate in transforms)
            {
                if (candidate.name == "Restaurantv3")
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }

    private static void CloseIfNeeded(
        Scene scene,
        bool openedAdditively
    )
    {
        if (openedAdditively && scene.IsValid())
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
