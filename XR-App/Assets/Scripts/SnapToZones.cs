using System.Collections.Generic;
using UnityEngine;

public class SnapToZones : MonoBehaviour
{
    private List<SnapZone> _snapZones = new List<SnapZone>();
    public float snapRadius = 0.5f;
    private Vector3 _originalPosition;

    void Start()
    {
        _snapZones = new(FindObjectsByType<SnapZone>(FindObjectsSortMode.None));
    }

    public void OnGrab()
    {
        _originalPosition = transform.position;
    }


    public void OnRelease()
    {
        Transform nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var zone in _snapZones)
        {
            float dist = Vector3.Distance(transform.position, zone.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = zone.transform;
            }
        }

        if (nearest != null && minDist <= snapRadius)
        {
            transform.position = new Vector3(_originalPosition.x, nearest.position.y + 0.1f, nearest.position.z);
        }
        else
        {
            transform.position = _originalPosition;
        }
    }
}