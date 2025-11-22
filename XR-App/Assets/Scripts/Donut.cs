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
    private Rigidbody? _rigidbody;

    private bool _canGrab = true;

    private Callout? _toolTip;
    private TextMeshProUGUI? _tooltipTextField;

    [SerializeField] private GameObject toolTipParent;

    void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
            _rigidbody = GetComponentInParent<Rigidbody>();
        ConfigureXRInteraction();
    }

    // ---------------- Configure XR Interaction ---------------- 
    
    private void ConfigureXRInteraction()
    {
        // Get the XR Grab Interactable component
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null)
            grabInteractable = GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        
        if (grabInteractable != null)
        {
            // Get the Rigidbody component
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = GetComponentInParent<Rigidbody>();
            
            // If Rigidbody is kinematic, disable throw on detach
            if (rb != null && rb.isKinematic)
            {
                grabInteractable.throwOnDetach = false;
            }
        }
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

    private void SetPosition(Vector3 position)
    {
        // Set position immediately first
        ApplyPosition(position);
        
        // Also set it after the frame to ensure XR system doesn't override it
        StartCoroutine(SetPositionCoroutine(position));
    }

    private void ApplyPosition(Vector3 position)
    {
        // Use Rigidbody.MovePosition for kinematic rigidbodies to ensure proper position setting
        if (_rigidbody != null && _rigidbody.isKinematic)
        {
            _rigidbody.MovePosition(position);
            _rigidbody.position = position;
        }
        
        // Always set transform.position directly to ensure it's updated
        transform.position = position;
    }

    private System.Collections.IEnumerator SetPositionCoroutine(Vector3 position)
    {
        // Wait for end of frame to ensure XR system has finished its release logic
        yield return new WaitForEndOfFrame();
        
        // Apply position again to ensure it persists
        ApplyPosition(position);
    }

    // ---------------- CanRelease ----------------

    private bool CanRelease(Tower? targetTower)
    {
        if (!_gameManager.isGameActive)
            return false;

        if (targetTower == null)
            return false;

        // Get all donuts on the target tower (excluding this one)
        var donutsInTower = _gameManager.GetDonutsInTower(targetTower);
        donutsInTower.Remove(this);

        // If tower is empty, can always place
        if (donutsInTower.Count == 0)
            return true;

        // Can only place if this donut is smaller than the top donut
        Donut topDonut = donutsInTower.OrderByDescending(d => d.transform.position.y).First();
        return topDonut.transform.localScale.magnitude > transform.localScale.magnitude;
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

        // Find the target tower (where we're trying to place the donut)
        Tower? targetTower = null;
        if (nearest != null)
        {
            // First, check if the snap zone is a child of a tower
            targetTower = nearest.GetComponentInParent<Tower>();
            
            // If not found, find the nearest tower by position
            if (targetTower == null)
            {
                targetTower = _gameManager.towers
                    .FirstOrDefault(t => Mathf.Abs(t.transform.position.z - nearest.position.z) < 0.4f);
            }
        }

        // Check if we can release on the target tower
        if (!_canGrab || !CanRelease(targetTower))
        {
            _gameManager.OnGrabFailed();
            SetPosition(_originalPosition);
            return;
        }

        if (nearest != null && minDist <= _snapRadius && targetTower != null)
        {
            // Get all donuts already on the target tower (excluding this one)
            var donutsOnTower = _gameManager.GetDonutsInTower(targetTower);
            donutsOnTower.Remove(this);
            
            float baseY = targetTower.transform.position.y;
            const float baseOffset = 0.1f;
            
            Vector3 newPosition;
            
            if (donutsOnTower.Count > 0)
            {
                // Find the top donut
                Donut topDonut = donutsOnTower.OrderByDescending(d => d.transform.position.y).First();
                
                // Get the actual height of the top donut
                Renderer topRenderer = topDonut.GetComponentInChildren<Renderer>();
                float topDonutHeight = topRenderer != null ? topRenderer.bounds.size.y : topDonut.transform.localScale.y;
                
                // Position this donut on top of the top donut
                newPosition = new Vector3(
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
                
                newPosition = new Vector3(
                    nearest.position.x,
                    baseY + baseOffset + donutHeight * 0.5f,
                    nearest.position.z
                );
            }
            
            SetPosition(newPosition);
        }
        else
        {
            SetPosition(_originalPosition);
        }

        if (_gameManager.IsGameEnd())
            _gameManager.OnGameEnd();
    }
}
