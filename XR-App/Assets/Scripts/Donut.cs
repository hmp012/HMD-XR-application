using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Donut : SnapZone
{
    public List<SnapZone> snapZones = new();
    private readonly float _snapRadius = 0.5f;
    private Vector3 _originalPosition;
    [SerializeField] public DonutsOrder donutsOrder;
    private bool _canGrab = true;

    public void OnGrab()
    {
        snapZones = new(FindObjectsByType<SnapZone>(FindObjectsSortMode.None));
        snapZones.RemoveAll(z => z.gameObject == gameObject);
        donutsOrder.OnGrab();

        _originalPosition = transform.position;
        _canGrab = CanGrab();
    }

    private bool CanGrab()
    {
        if (!donutsOrder.IsOrderCorrect(transform.position.z))
        {
            return false;
        }

        Debug.Log(donutsOrder.IsOrderCorrect(transform.position.z));

        var objectsInOrder = donutsOrder.GetObjectsInOrder(transform.position.z);
        return objectsInOrder!.First().transform == transform;
    }

    public void OnRelease()
    {
        Transform nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var zone in snapZones)
        {
            float dist = Vector3.Distance(transform.position, zone.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = zone.transform;
            }
        }

        if (!_canGrab)
        {
            donutsOrder.OnGrabFailed();
            return;
        }

        if (nearest != null && minDist <= _snapRadius && _canGrab)
        {
            transform.position = new Vector3(_originalPosition.x, nearest.position.y + 0.1f, nearest.position.z);
        }
        else
        {
            transform.position = _originalPosition;
        }
    }
}