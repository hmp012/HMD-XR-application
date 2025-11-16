using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DefaultNamespace;
using JetBrains.Annotations;
using UnityEngine.Serialization;

public class DonutsOrder : MonoBehaviour
{
    public List<Donut> objectsToTrack;
    public List<Tower> towers;
    private List<Vector3> _originalOrder;

    void Start()
    {
        objectsToTrack = gameObject.scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Donut>())
            .OrderBy(o => o.transform.localScale.magnitude)
            .ToList();
        towers = gameObject.scene.GetRootGameObjects()
            .SelectMany(s => s.GetComponentsInChildren<Tower>())
            .OrderBy(o => o.transform.position.z)
            .ToList();
    }

    public void OnGrab()
    {
        _originalOrder = objectsToTrack
            .Select(o => o.transform.position)
            .ToList();
        Debug.Log("Original position saved");
    }

    public void OnGrabFailed()
    {
        for (var i = 0; i < objectsToTrack.Count; i++)
        {
            objectsToTrack[i].transform.position = _originalOrder[i];
            Debug.Log(objectsToTrack[i].transform.position + " reset to " + _originalOrder[i]);
        }
    }

    [CanBeNull]
    public Tower GetTower(Donut donut)
    {
        return towers
            .FirstOrDefault(t => NearlyEqual(t.transform.position.z, donut.transform.position.z));
    }

    [CanBeNull]
    public List<Donut> GetDonutsInTower(Tower tower)
    {
        return objectsToTrack
            .Where(o => NearlyEqual(o.transform.position.z, tower.transform.position.z))
            .OrderBy(o => o.transform.position.y)
            .ToList();
    }

    [CanBeNull]
    public Donut[] GetObjectsInOrder(float forZ)
    {
        Debug.Log(objectsToTrack
            .Where(o => NearlyEqual(objectsToTrack[2].transform.position.z, forZ))
            .OrderByDescending(obj => obj.transform.position.y)
            .ToArray());
        return IsOrderCorrect(forZ)
            ? objectsToTrack
                .Where(o => NearlyEqual(o.transform.position.z, forZ))
                .OrderByDescending(obj => obj.transform.position.y)
                .ToArray()
            : null;
    }

    public bool IsOrderCorrect(float forZ)
    {
        Donut[] objectsInOrder = objectsToTrack
            .Where(o => NearlyEqual(o.transform.position.z, forZ))
            .OrderByDescending(obj => obj.transform.position.y)
            .ToArray();
        Donut[] donutsInMagnitudeOrder = objectsInOrder
            .OrderBy(o => o.transform.localScale.magnitude)
            .ToArray();

        for (int i = 0; i < objectsInOrder.Length; i++)
        {
            Debug.Log(objectsInOrder[i].name + " vs " + donutsInMagnitudeOrder[i].name);
            if (!objectsInOrder[i].name.Equals(donutsInMagnitudeOrder[i].name))
                return false;
        }

        return true;
    }

    public void PrintOrder(float forZ)
    {
        var ordered = GetObjectsInOrder(forZ);
        Debug.Log("Bottom → Top: " + string.Join(", ", ordered.Select(o => o.name)));
    }

    public static bool NearlyEqual(float a, float b, float epsilon = 0.4f)
    {
        return Mathf.Abs(a - b) <= epsilon;
    }
}