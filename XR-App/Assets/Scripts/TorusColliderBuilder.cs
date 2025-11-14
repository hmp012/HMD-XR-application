using UnityEngine;
using UnityEditor;

public static class TorusColliderBuilder
{
    private const string MenuName = "Tools/Build Torus Colliders";

    [MenuItem(MenuName)]
    private static void BuildTorusColliders()
    {
        GameObject obj = Selection.activeGameObject;

        if (!obj)
        {
            Debug.LogError("Select a torus object first.");
            return;
        }

        MeshFilter mf = obj.GetComponent<MeshFilter>();
        if (!mf)
        {
            Debug.LogError("Selected object has no MeshFilter.");
            return;
        }

        Mesh mesh = mf.sharedMesh;

        if (!mesh)
        {
            Debug.LogError("Torus mesh not found.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(obj, "Build Torus Colliders");

        // Remove existing capsule colliders
        foreach (var col in obj.GetComponents<CapsuleCollider>())
            Undo.DestroyObjectImmediate(col);

        // --- AUTO DETECT THE RADII ---
        Vector3[] verts = mesh.vertices;

        float majorRadius = 0f;
        float minorRadius = Mathf.Infinity;

        // Compute average distance from center for major radius
        foreach (var v in verts)
        {
            Vector2 flat = new Vector2(v.x, v.z);
            float dist = flat.magnitude;
            majorRadius += dist;
        }
        majorRadius /= verts.Length;

        // Compute distance from tube center for minor radius
        foreach (var v in verts)
        {
            Vector2 flat = new Vector2(v.x, v.z);
            float distToTubeCenter = Mathf.Abs(flat.magnitude - majorRadius);
            if (distToTubeCenter < minorRadius)
                minorRadius = distToTubeCenter;
        }

        // Model scale-aware radii
        majorRadius *= obj.transform.localScale.x;
        minorRadius *= obj.transform.localScale.x;

        // --- COLLIDER GENERATION ---
        int segmentCount = 16; // You can increase this for smoother collision

// Create sibling "Colliders" object
        GameObject colliderParent = new GameObject(obj.name + "_Colliders");
        colliderParent.transform.SetParent(obj.transform.parent, false); // sibling
        colliderParent.transform.position = obj.transform.position;
        colliderParent.transform.rotation = obj.transform.rotation;
        colliderParent.transform.localScale = obj.transform.localScale;

// Remove old capsules in this parent if any
        foreach (var col in colliderParent.GetComponentsInChildren<CapsuleCollider>())
            Undo.DestroyObjectImmediate(col);
        
        for (int i = 0; i < segmentCount; i++)
        {
            float angle = (float)i / segmentCount * Mathf.PI * 2f;

            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * majorRadius,
                0,
                Mathf.Sin(angle) * majorRadius
            );

            // Create capsule as child of colliderParent
            GameObject capsuleObj = new GameObject("CapsuleCollider_" + i);
            capsuleObj.transform.SetParent(colliderParent.transform, false);
            capsuleObj.transform.localPosition = pos;
            capsuleObj.transform.localRotation = Quaternion.Euler(90f, -angle * Mathf.Rad2Deg, 0f);
            capsuleObj.transform.localScale = Vector3.one;

            CapsuleCollider cc = capsuleObj.AddComponent<CapsuleCollider>();
            cc.direction = 1; // vertical Y axis
            cc.radius = minorRadius * 0.9f;
            cc.height = majorRadius * 2f;
        }



        Debug.Log($"Generated colliders. Major radius: {majorRadius}, Minor radius: {minorRadius}");
    }
}
