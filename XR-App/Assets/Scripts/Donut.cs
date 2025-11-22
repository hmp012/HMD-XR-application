#nullable enable
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class Donut : SnapZone
{
    private List<SnapZone> _snapZones = new();
    private readonly float _snapRadius = 0.5f;

    private Vector3 _originalPosition;
    private GameManager _gameManager;

    private bool _canGrab = true;

    private Callout? _toolTip;
    private TextMeshProUGUI? _tooltipTextField;

    [SerializeField] private GameObject toolTipParent;

    void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    // ---------------- Tooltip Creation ----------------

    private void CreateToolTip()
    {
        Callout source = FindObjectsByType<Callout>(FindObjectsSortMode.None)
            .FirstOrDefault(c => c.name == "ToolTip");

        if (source == null)
            return;

        GameObject instance = Instantiate(source.gameObject, toolTipParent.transform, true);
        _toolTip = instance.GetComponent<Callout>();
        if (_toolTip == null)
            return;

        _toolTip.gameObject.SetActive(true);
        _toolTip.enabled = true;
        _toolTip.TurnOffStuff();
        _toolTip.name = "ToolTip " + name;

        BezierCurve curve = _toolTip.GetComponentInChildren<BezierCurve>(true);
        if (curve != null)
        {
            curve.m_StartPoint = transform;
            curve.m_EndPoint = _toolTip.transform;
        }

        LazyFollow lazy = FindFirstObjectByType<LazyFollow>();
        if (lazy == null)
            return;

        lazy.transform.SetParent(_toolTip.transform);
        _tooltipTextField = lazy.GetComponent<TextMeshProUGUI>();
    }

    // ---------------- OnGrab ----------------

    public void OnGrab()
    {
        CreateToolTip();
        _gameManager = FindFirstObjectByType<GameManager>();

        _snapZones = new List<SnapZone>(FindObjectsByType<SnapZone>(FindObjectsSortMode.None));
        _snapZones.RemoveAll(z => z.gameObject == gameObject);

        _gameManager.OnGrab();

        _originalPosition = transform.position;
        _canGrab = CanGrab();
    }

    // ---------------- CanGrab ----------------

    private bool CanGrab()
    {
        if (!_gameManager.isGameActive)
            return false;

        Tower tower = _gameManager.GetTower(this);
        if (tower == null)
            return false;

        // Get all donuts on this tower, ordered by Y position (bottom to top)
        var donutsOnTower = _gameManager.GetDonutsInTower(tower);
        
        if (donutsOnTower.Count == 0)
            return true; // No donuts on tower, can grab
        
        // The top donut is the one with the highest Y position
        Donut topDonut = donutsOnTower.OrderByDescending(d => d.transform.position.y).First();
        
        bool isTopDonut = topDonut == this;
        
        if (!isTopDonut)
        {
            _tooltipTextField?.SetText($"{name} is not the top donut!");
            _toolTip?.TurnOnStuff();
            return false;
        }

        return true;
    }

    // ---------------- Helper Methods ----------------

    private float GetDonutHeight()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        return renderer != null ? renderer.bounds.size.y : transform.localScale.y;
    }

    // ---------------- CanRelease ----------------

    private bool CanRelease()
    {
        if (!_gameManager.isGameActive)
            return false;

        Tower tower = _gameManager.GetTower(this);
        if (tower == null)
            return false;

        var donutsInTower = _gameManager.GetDonutsInTower(tower);
        if (donutsInTower.Count == 0)
            return true;

        donutsInTower.Remove(this);

        if (donutsInTower.Count == 0)
            return true;

        return donutsInTower.Last().transform.localScale.magnitude > transform.localScale.magnitude;
    }

    // ---------------- OnRelease ----------------

    public void OnRelease()
    {
        _tooltipTextField?.SetText("");
        _toolTip?.TurnOffStuff();

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

        if (!_canGrab || !CanRelease())
        {
            _gameManager.OnGrabFailed();
            transform.position = _originalPosition;
            return;
        }

        if (nearest != null && minDist <= _snapRadius)
        {
            // Get the tower for this snap zone
            Tower tower = _gameManager.GetTower(this);
            if (tower != null)
            {
                // Get all donuts already on this tower
                var donutsOnTower = _gameManager.GetDonutsInTower(tower);
                donutsOnTower.Remove(this); // Remove self from list
                
                float baseY = tower.transform.position.y;
                const float baseOffset = 0.1f;
                
                if (donutsOnTower.Count > 0)
                {
                    // Find the top donut
                    Donut topDonut = donutsOnTower.OrderByDescending(d => d.transform.position.y).First();
                    
                    // Get the actual height of the top donut
                    Renderer topRenderer = topDonut.GetComponentInChildren<Renderer>();
                    float topDonutHeight = topRenderer != null ? topRenderer.bounds.size.y : topDonut.transform.localScale.y;
                    
                    // Position this donut on top of the top donut
                    transform.position = new Vector3(
                        nearest.position.x,
                        topDonut.transform.position.y + topDonutHeight * 0.5f + (GetDonutHeight() * 0.5f),
                        nearest.position.z
                    );
                }
                else
                {
                    // No donuts on tower, place at base
                    Renderer renderer = GetComponentInChildren<Renderer>();
                    float donutHeight = renderer != null ? renderer.bounds.size.y : transform.localScale.y;
                    
                    transform.position = new Vector3(
                        nearest.position.x,
                        baseY + baseOffset + donutHeight * 0.5f,
                        nearest.position.z
                    );
                }
            }
            else
            {
                // Fallback to old method if tower not found
                transform.position = new Vector3(
                    nearest.position.x,
                    nearest.position.y + 0.1f,
                    nearest.position.z
                );
            }
        }
        else
        {
            transform.position = _originalPosition;
        }

        if (_gameManager.IsGameEnd())
            _gameManager.OnGameEnd();
    }
}
