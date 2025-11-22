using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public List<Donut> objectsToTrack = new();
    public List<Tower> towers = new();

    public bool isGameActive = false;

    public TextMeshProUGUI winText;

    // Add these missing fields
    [SerializeField] private int _numberOfDonuts = 0;
    [SerializeField] private TextMeshProUGUI mStepButtonTextField;
    [SerializeField] private Donut originalDonut;
    [SerializeField] private GameObject donutsParent;

    void Start()
    {
        if (winText != null)
            winText.gameObject.SetActive(false);

        DetectSceneObjects();
        
        // Auto-assign originalDonut if not set
        if (originalDonut == null && objectsToTrack.Count > 0)
        {
            originalDonut = objectsToTrack.FirstOrDefault(d => d.name.Contains("Donut 1") || d.name == "Donut 1");
            if (originalDonut == null)
                originalDonut = objectsToTrack.First();
            
            Debug.Log($"Auto-assigned originalDonut: {originalDonut.name}");
        }
        
        // Auto-assign donutsParent if not set
        if (donutsParent == null && originalDonut != null)
        {
            donutsParent = originalDonut.transform.parent?.gameObject;
            if (donutsParent == null)
            {
                // Try to find "Interactables" GameObject
                donutsParent = GameObject.Find("Interactables");
            }
            
            if (donutsParent != null)
                Debug.Log($"Auto-assigned donutsParent: {donutsParent.name}");
            else
                Debug.LogWarning("Could not auto-assign donutsParent. Please assign manually in Inspector.");
        }
        
        // Initialize number of donuts from existing donuts
        _numberOfDonuts = objectsToTrack.Count;
        if (mStepButtonTextField != null)
            mStepButtonTextField.text = _numberOfDonuts.ToString();
    }

    // ---------------- Detect Donuts & Towers ----------------

    private void DetectSceneObjects()
    {
        // Find all donuts in scene
        objectsToTrack = gameObject.scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Donut>())
            .ToList();

        // Find all towers in scene
        towers = gameObject.scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Tower>())
            .OrderBy(t => t.transform.position.z)   // Sort by Z
            .ToList();

        Debug.Log($"Found {towers.Count} towers");
        foreach (var t in towers)
            Debug.Log($"{t.name}: Z={t.transform.position.z}");
    }

    // ---------------- Game Start ----------------

    public void OnGameStart()
    {
        DetectSceneObjects();
        isGameActive = true;
        InitializeDonutPositions();
        Debug.Log("GAME STARTED");
    }

    // ---------------- Initialize Donut Positions ----------------

    private void InitializeDonutPositions()
    {
        if (towers.Count == 0 || objectsToTrack.Count == 0)
            return;

        // Get the first tower (leftmost)
        Tower firstTower = towers.First();
        Vector3 towerPos = firstTower.transform.position;

        // Sort donuts by scale (largest to smallest) - largest at bottom, smallest at top
        var sortedDonuts = objectsToTrack
            .OrderByDescending(d => d.transform.localScale.x)
            .ToList();

        // Position donuts on the first tower, starting from the tower base
        float currentY = towerPos.y;
        const float baseOffset = 0.1f; // Base offset from tower
        currentY += baseOffset;

        for (int i = 0; i < sortedDonuts.Count; i++)
        {
            Donut donut = sortedDonuts[i];
            
            // Get the actual height of the donut (using bounds or scale)
            Renderer renderer = donut.GetComponentInChildren<Renderer>();
            float donutHeight = renderer != null ? renderer.bounds.size.y : donut.transform.localScale.y;
            
            // Position the donut (center it vertically at currentY)
            donut.transform.position = new Vector3(
                towerPos.x,
                currentY + donutHeight * 0.5f,
                towerPos.z
            );

            // Update currentY for next donut (stack on top)
            currentY += donutHeight;
        }
    }

    // ---------------- Game End ----------------

    public void OnGameEnd()
    {
        Debug.Log("YOU WIN!!!");

        if (winText != null)
        {
            winText.gameObject.SetActive(true);
            winText.text = "YOU WIN!";
        }
    }

    // ---------------- WIN CHECK ----------------

    public bool IsGameEnd()
    {
        DetectSceneObjects();

        Tower targetTower = towers.Last(); // Always the RIGHT-most tower

        var donutsOnTarget = GetDonutsInTower(targetTower);

        return donutsOnTarget.Count == objectsToTrack.Count;
    }

    // ---------------- Tower Helpers ----------------

    public Tower GetTower(Donut donut)
    {
        return towers.FirstOrDefault(t =>
            Mathf.Abs(t.transform.position.z - donut.transform.position.z) < 0.4f);
    }

    public List<Donut> GetDonutsInTower(Tower tower)
    {
        return objectsToTrack
            .Where(d => Mathf.Abs(d.transform.position.z - tower.transform.position.z) < 0.4f)
            .OrderBy(d => d.transform.position.y)
            .ToList();
    }

    // ---------------- Order Check ----------------

    public bool IsOrderCorrect(float z)
    {
        var donuts = objectsToTrack
            .Where(d => Mathf.Abs(d.transform.position.z - z) < 0.4f)
            .OrderByDescending(d => d.transform.position.y)
            .ToList();

        var expected = donuts.OrderBy(d => d.transform.localScale.x).ToList();

        if (donuts.Count != expected.Count)
            return false;

        for (int i = 0; i < donuts.Count; i++)
            if (donuts[i].name != expected[i].name)
                return false;

        return true;
    }

    public Donut[] GetObjectsInOrder(float z)
    {
        var donuts = objectsToTrack
            .Where(d => Mathf.Abs(d.transform.position.z - z) < 0.4f)
            .OrderByDescending(d => d.transform.position.y)
            .ToArray();

        var correct = donuts.OrderBy(d => d.transform.localScale.x).ToArray();

        for (int i = 0; i < donuts.Length; i++)
            if (donuts[i].name != correct[i].name)
                return null;

        return donuts;
    }

    public void OnGrab() => DetectSceneObjects();
    public void OnGrabFailed() => Debug.Log("Grab failed → reset donut");

    // Add these missing methods:

    public void AddDonut()
    {
        SetNumberOfDonuts(1, false);
    }

    public void SetNumberOfDonuts(int number, bool isActive)
    {
        if (mStepButtonTextField != null)
            mStepButtonTextField.text = _numberOfDonuts.ToString();

        if (number > 0)
        {
            for (int i = 0; i < number; i++)
            {
                _numberOfDonuts++;
                Donut newDonut = CreateNewDonut(isActive);
                if (newDonut != null)
                    newDonut.enabled = false;
            }
        }
        else
        {
            for (int i = 0; i > number; i--)
            {
                if (_numberOfDonuts > 1)
                {
                    _numberOfDonuts--;
                    RemoveDonut();
                }
            }
        }

        UpdateObjectsToTrack();
        if (mStepButtonTextField != null)
            mStepButtonTextField.text = _numberOfDonuts.ToString();
    }

    public Donut CreateNewDonut(bool isActive)
    {
        // Try to auto-assign if still null
        if (originalDonut == null)
        {
            DetectSceneObjects();
            if (objectsToTrack.Count > 0)
            {
                originalDonut = objectsToTrack.FirstOrDefault(d => d.name.Contains("Donut 1") || d.name == "Donut 1");
                if (originalDonut == null)
                    originalDonut = objectsToTrack.First();
            }
        }
        
        if (donutsParent == null && originalDonut != null)
        {
            donutsParent = originalDonut.transform.parent?.gameObject;
            if (donutsParent == null)
                donutsParent = GameObject.Find("Interactables");
        }
        
        if (originalDonut == null || donutsParent == null)
        {
            Debug.LogError($"originalDonut or donutsParent is not assigned! originalDonut: {originalDonut}, donutsParent: {donutsParent}");
            return null;
        }

        Donut newDonut = Instantiate(originalDonut, donutsParent.transform, true);
        
        // Set position based on original donut's position
        Vector3 originalPos = originalDonut.transform.localPosition;
        
        // Calculate proper spacing based on donut height
        Renderer originalRenderer = originalDonut.GetComponentInChildren<Renderer>();
        float donutHeight = originalRenderer != null ? originalRenderer.bounds.size.y : originalDonut.transform.localScale.y;
        
        newDonut.transform.localPosition = new Vector3(
            originalPos.x,
            originalPos.y + donutHeight * (_numberOfDonuts - 1),
            originalPos.z
        );

        // Set scale (smaller for each new donut)
        Vector3 originalScale = originalDonut.transform.localScale;
        float scaleReduction = 0.1f * (_numberOfDonuts - 1);
        newDonut.transform.localScale = new Vector3(
            originalScale.x - scaleReduction,
            originalScale.y - scaleReduction,
            originalScale.z - scaleReduction
        );

        newDonut.enabled = isActive;
        newDonut.gameObject.SetActive(true);
        newDonut.name = originalDonut.name + _numberOfDonuts.ToString();

        // Set color gradient (red to blue based on number)
        MeshRenderer renderer = newDonut.GetComponentInChildren<MeshRenderer>();
        if (renderer != null && renderer.material != null)
        {
            float t = (float)(_numberOfDonuts - 1) / 5f;
            Color color = Color.Lerp(Color.red, Color.blue, t);
            renderer.material.SetColor("_BaseColor", color);
        }

        return newDonut;
    }

    public void RemoveDonut()
    {
        UpdateObjectsToTrack();
        
        if (_numberOfDonuts > 1 && objectsToTrack.Count > 0)
        {
            // Find and remove the last created donut (highest number)
            Donut toRemove = objectsToTrack
                .Where(d => d.name.StartsWith(originalDonut.name))
                .OrderByDescending(d => d.name)
                .FirstOrDefault();

            if (toRemove != null)
            {
                objectsToTrack.Remove(toRemove);
                Destroy(toRemove.gameObject);
            }
        }
    }

    public void UpdateObjectsToTrack()
    {
        objectsToTrack = gameObject.scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Donut>())
            .Where(d => d != originalDonut) // Exclude the original template
            .OrderBy(d => d.transform.position.z)
            .ThenBy(d => d.transform.position.y)
            .ToList();
    }

    public int GetNumberOfDonuts()
    {
        return _numberOfDonuts;
    }
}
