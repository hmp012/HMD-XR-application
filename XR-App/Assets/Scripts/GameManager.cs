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

    void Start()
    {
        if (winText != null)
            winText.gameObject.SetActive(false);

        DetectSceneObjects();
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
        Debug.Log("GAME STARTED");
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
}
