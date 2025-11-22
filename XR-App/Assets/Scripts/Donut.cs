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
    private Callout _toolTip;
    private TextMeshProUGUI _tooltipTextField;
    [SerializeField] private GameObject toolTipParent;

    void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    private void CreateToolTip()
    {
        List<Callout> callouts = new(FindObjectsByType<Callout>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        var callout = callouts.FirstOrDefault(c => c.gameObject.name.Equals("ToolTip"));
        if (callout != null)
        {
            GameObject toolTipObject = Instantiate(callout.gameObject, toolTipParent.transform, true);
            _toolTip = toolTipObject.GetComponent<Callout>();

            _toolTip.gameObject.SetActive(true);
            _toolTip.enabled = true;
            _toolTip.name = "ToolTip " + name;
            _toolTip.transform.position = transform.position + new Vector3(-1, 1, 0) * 0.5f;
            
            LazyFollow lazyFollow = _toolTip.GetComponentInChildren<LazyFollow>(true);
            if (lazyFollow != null)
            {
                lazyFollow.target.position = transform.position + new Vector3(-1, 1, 0) * 0.5f;
            }

            BezierCurve? bezierCurve = _toolTip.GetComponentInChildren<BezierCurve>(true);
            if (bezierCurve != null && lazyFollow != null)
            {
                bezierCurve.m_StartPoint = transform;
                bezierCurve.m_EndPoint = lazyFollow.target;
            }

            _tooltipTextField = toolTipObject.GetComponentInChildren<TextMeshProUGUI>();
            _toolTip.TurnOffStuff();
            _toolTip.gameObject.SetActive(false);
        }
    }


    public void OnGrab()
    {
        // if (_toolTip == null)
        // {
        //     CreateToolTip();
        // }

        _gameManager = FindFirstObjectByType<GameManager>();
        _snapZones = new(FindObjectsByType<SnapZone>(FindObjectsSortMode.None));
        _snapZones.RemoveAll(z => z.gameObject == gameObject);
        _gameManager.OnGrab();

        _originalPosition = transform.position;
        _canGrab = CanGrab();
    }

    private bool CanGrab()
    {
        if (!_gameManager.IsOrderCorrect(transform.position.z) || !_gameManager.isGameActive)
        {
            _toolTip.gameObject.SetActive(true);
            _tooltipTextField.text = gameObject.name + " cannot be grabbed right now.";
            _toolTip.TurnOnStuff();
            transform.position = _originalPosition;
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
        if (_tooltipTextField != null)
        {
            _tooltipTextField.text = "";
            _toolTip.TurnOffStuff();
            _toolTip.gameObject.SetActive(false);
        }

        if (!_canGrab)
        {
            _gameManager.OnGrabFailed();
            return;
        }
        
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

        if (!CanRelease())
        {
            _gameManager.OnGrabFailed();
            return;
        }

        if (nearest != null && minDist <= _snapRadius && _canGrab)
        {
            transform.position = new Vector3(_originalPosition.x, nearest.position.y + 0.1f, nearest.position.z);
            _gameManager.stepCount++;
            _gameManager.mNumberOfMovesTextField.text = $"Number of moves: {_gameManager.stepCount}";
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