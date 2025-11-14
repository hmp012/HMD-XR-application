using UnityEngine;
using UnityEditor;

public static class TorusColliderBuilder
{
    private const string MenuName = "Tools/Build Torus Colliders";

    [MenuItem(MenuName)]
    private static void BuildTorusColliders()
    {
        GameObject obj = Selection.activeGameObject;
        GameObject parentObj = obj ? obj.transform.parent?.gameObject : null;

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
        float minY = Mathf.Infinity;
        float maxY = Mathf.NegativeInfinity;

        // Compute average distance from center for major radius and track Y bounds
        foreach (var v in verts)
        {
            Vector2 flat = new Vector2(v.x, v.z);
            float dist = flat.magnitude;
            majorRadius += dist;

            // Track min/max Y in local space
            if (v.y < minY) minY = v.y;
            if (v.y > maxY) maxY = v.y;
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

        // Model scale-aware values
        majorRadius *= obj.transform.localScale.x;
        minorRadius *= obj.transform.localScale.x;
        float meshHeight = (maxY - minY) * obj.transform.localScale.y;
        float heightOffset = meshHeight * 0.5f;


        // --- COLLIDER GENERATION ---
        int segmentCount = 50;

        for (int j = 0; j < 5; j++)
        {
            GameObject colliderParent = new GameObject(obj.name + "_Colliders_" + j);
            colliderParent.transform.SetParent(obj.transform.parent, true);
            if (parentObj != null && j%2 == 1)
            {
                colliderParent.transform.localPosition = new Vector3(0, 0, (float)(0.1/j));
            }
            else if (parentObj != null && j%2 == 0)
            {
                colliderParent.transform.localPosition = new Vector3(0, 0, (float)(-0.1/j));
            }

            colliderParent.transform.localEulerAngles = new Vector3(90, 0, 0);
            colliderParent.transform.localScale = Vector3.one;

            
            
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = (float)i / segmentCount * Mathf.PI * 2f;

                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * majorRadius,
                    0,
                    Mathf.Sin(angle) * majorRadius
                );

                GameObject capsuleObj = new GameObject($"CapsuleCollider_Ring_{i}");
                capsuleObj.transform.SetParent(colliderParent.transform, true);
                capsuleObj.transform.localPosition = pos;
                capsuleObj.transform.localScale = Vector3.one;
                capsuleObj.transform.localRotation = Quaternion.Euler(90f, -angle * Mathf.Rad2Deg, 0f);

                CapsuleCollider cc = capsuleObj.AddComponent<CapsuleCollider>();
                cc.direction = 1;
                cc.radius = minorRadius * 0.2f;
                cc.height = majorRadius * 3;
            }
        }


        Debug.Log($"Generated colliders. Major radius: {majorRadius}, Minor radius: {minorRadius}");
    }
}