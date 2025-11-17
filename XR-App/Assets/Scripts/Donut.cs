#nullable enable
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

public class Donut : SnapZone
{
    private List<SnapZone> _snapZones = new();
    private readonly float _snapRadius = 0.5f;
    private Vector3 _originalPosition;
    private GameManager _gameManager;
    private bool _canGrab = true;

    void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    public void OnGrab()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
        _snapZones = new(FindObjectsByType<SnapZone>(FindObjectsSortMode.None));
        _snapZones.RemoveAll(z => z.gameObject == gameObject);
        _gameManager.OnGrab();

        _originalPosition = transform.position;
        _canGrab = CanGrab();
    }

    private bool CanGrab()
    {
        if (!_gameManager.IsOrderCorrect(transform.position.z) && !_gameManager.isGameActive)
        {
            return false;
        }

        var objectsInOrder = _gameManager.GetObjectsInOrder(transform.position.z);
        return objectsInOrder!.First().transform == transform;
    }

    private bool CanRelease()
    {
        if (!_gameManager.isGameActive)
        {
            return false;
        }
        
        Tower? tower = _gameManager.GetTower(this);
        if (tower == null)
        {
            return false;
        }

        var donutsInTower = _gameManager.GetDonutsInTower(tower);
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
        Transform? nearest = null;
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

        if (!_canGrab)
        {
            _gameManager.OnGrabFailed();
            return;
        }

        if (!CanRelease())
        {
            _gameManager.OnGrabFailed();
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

        if (_gameManager.IsGameEnd())
        {
            _gameManager.OnGameEnd();
        }
    }
}