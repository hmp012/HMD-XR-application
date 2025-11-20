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

        var objectsInOrder = _gameManager.GetObjectsInOrder(tower.transform.position.z);
        if (objectsInOrder == null)
        {
            _tooltipTextField?.SetText($"{name} is not the top donut!");
            _toolTip?.TurnOnStuff();
            return false;
        }

        return objectsInOrder.First().transform == transform;
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
            transform.position = new Vector3(
                nearest.position.x,
                nearest.position.y + 0.1f,
                nearest.position.z
            );
        }
        else
        {
            transform.position = _originalPosition;
        }

        if (_gameManager.IsGameEnd())
            _gameManager.OnGameEnd();
    }
}
