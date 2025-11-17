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
    [SerializeField] public GameManager gameManager;
    private bool _canGrab = true;

    public void OnGrab()
    {
        snapZones = new(FindObjectsByType<SnapZone>(FindObjectsSortMode.None));
        snapZones.RemoveAll(z => z.gameObject == gameObject);
        gameManager.OnGrab();

        _originalPosition = transform.position;
        _canGrab = CanGrab();
    }

    private bool CanGrab()
    {
        if (!gameManager.IsOrderCorrect(transform.position.z))
        {
            return false;
        }
        
        var objectsInOrder = gameManager.GetObjectsInOrder(transform.position.z);
        return objectsInOrder!.First().transform == transform;
    }
    
    private bool CanRelease()
    {
        var tower = gameManager.GetTower(this);
        if (tower == null)
        {
            return false;
        }

        var donutsInTower = gameManager.GetDonutsInTower(tower);
        if (donutsInTower != null)
        {
            donutsInTower.RemoveAll(donut => donut == this);
        }
        if (donutsInTower == null || donutsInTower.Count == 0)
        {
            return true;
        }
        return donutsInTower.Last().transform.localScale.magnitude > transform.localScale.magnitude;
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
            gameManager.OnGrabFailed();
            return;
        }
        
        if (!CanRelease())
        {
            gameManager.OnGrabFailed();
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

        if (gameManager.IsGameEnd())
        {
            gameManager.OnGameEnd();
        }
    }
}