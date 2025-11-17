using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine.PlayerLoop;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GameManager : MonoBehaviour
{
    public List<Donut> objectsToTrack;
    public List<Tower> towers;
    private List<Vector3> _originalOrder;
    public List<XRGrabInteractable> interactables;
    private int _numberOfDonuts = 1;
    public TextMeshProUGUI mStepButtonTextField;
    public TextMeshProUGUI mContinueButtonTextField;
    [SerializeField] private Donut originalDonut;
    [SerializeField] private GameObject donutsParent;
    public bool isGameActive = false;

    void Start()
    {
    }

    private Donut CreateNewDonut(bool isActive = true)
    {
        var clone = Instantiate(originalDonut, donutsParent.transform, true);
        clone.transform.localPosition = new Vector3(originalDonut.transform.localPosition.x,
            originalDonut.transform.localPosition.y + 0.3f * (_numberOfDonuts - 1),
            originalDonut.transform.localPosition.z);
        clone.transform.localScale = new Vector3(originalDonut.transform.localScale.x - 0.1f * (_numberOfDonuts - 1),
            originalDonut.transform.localScale.y - 0.1f * (_numberOfDonuts - 1),
            originalDonut.transform.localScale.z - 0.1f * (_numberOfDonuts - 1));
        clone.enabled = isActive;
        clone.gameObject.SetActive(true);
        clone.name = originalDonut.name + _numberOfDonuts;
        return clone;
    }

    public void RemoveDonut()
    {
        UpdateObjectsToTrack();
        if (_numberOfDonuts <= 1 && objectsToTrack.Count <= 1)
            return;
        Debug.Log(objectsToTrack.First().name);
        Destroy(objectsToTrack.First().gameObject);
        _numberOfDonuts--;
        mStepButtonTextField.text = _numberOfDonuts.ToString();
        UpdateObjectsToTrack();
    }

    public void AddDonut()
    {
        SetNumberOfDonuts(1);
    }

    private void SetNumberOfDonuts(int number, bool isActive = false)
    {
        mStepButtonTextField.text = _numberOfDonuts.ToString();
        if (number > 0)
        {
            for (int i = 0; i < number; i++)
            {
                _numberOfDonuts++;
                var donut = CreateNewDonut(isActive);
                donut.enabled = false;
            }
        }
        else
        {
            for (int i = 0; i > number; i--)
            {
                _numberOfDonuts--;
                RemoveDonut();
            }
        }
        UpdateObjectsToTrack();
        mStepButtonTextField.text = _numberOfDonuts.ToString();
    }

    public int GetNumberOfDonuts()
    {
        return _numberOfDonuts;
    }

    private void UpdateObjectsToTrack()
    {
        objectsToTrack = gameObject.scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Donut>())
            .OrderBy(o => o.transform.localScale.magnitude)
            .ToList();
        towers = gameObject.scene.GetRootGameObjects()
            .SelectMany(s => s.GetComponentsInChildren<Tower>())
            .OrderBy(o => o.transform.position.z)
            .ToList();
    }

    public void OnGameStart()
    {
        UpdateObjectsToTrack();
        interactables = new(FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None));
        isGameActive = true;
        foreach (var interactable in interactables)
        {
            interactable.enabled = true;
        }
    }

    public void OnGameEnd()
    {
        foreach (var interactable in interactables)
        {
            interactable.enabled = false;
        }
    }

    public bool IsGameEnd()
    {
        var lastTower = towers
            .First();
        var donutsInLastTower = GetDonutsInTower(lastTower);
        return donutsInLastTower != null && donutsInLastTower.Count == objectsToTrack.Count;
    }

    public void OnGrab()
    {
        UpdateObjectsToTrack();
        _originalOrder = objectsToTrack
            .Select(o => o.transform.position)
            .ToList();
    }

    public void OnGrabFailed()
    {
        UpdateObjectsToTrack();
        for (var i = 0; i < objectsToTrack.Count; i++)
        {
            objectsToTrack[i].transform.position = _originalOrder[i];
        }
    }

    [CanBeNull]
    public Tower GetTower(Donut donut)
    {
        UpdateObjectsToTrack();
        return towers
            .FirstOrDefault(t => NearlyEqual(t.transform.position.z, donut.transform.position.z));
    }

    [CanBeNull]
    public List<Donut> GetDonutsInTower(Tower tower)
    {
        return objectsToTrack
            .Where(o => NearlyEqual(o.transform.position.z, tower.transform.position.z))
            .OrderBy(o => o.transform.position.y)
            .ToList();
    }

    [CanBeNull]
    public Donut[] GetObjectsInOrder(float forZ)
    {
        return IsOrderCorrect(forZ)
            ? objectsToTrack
                .Where(o => NearlyEqual(o.transform.position.z, forZ))
                .OrderByDescending(obj => obj.transform.position.y)
                .ToArray()
            : null;
    }

    public bool IsOrderCorrect(float forZ)
    {
        UpdateObjectsToTrack();
        Donut[] objectsInOrder = objectsToTrack
            .Where(o => NearlyEqual(o.transform.position.z, forZ))
            .OrderByDescending(obj => obj.transform.position.y)
            .ToArray();
        Donut[] donutsInMagnitudeOrder = objectsInOrder
            .OrderBy(o => o.transform.localScale.magnitude)
            .ToArray();

        for (int i = 0; i < objectsInOrder.Length; i++)
        {
            if (!objectsInOrder[i].name.Equals(donutsInMagnitudeOrder[i].name))
                return false;
        }

        return true;
    }

    public void PrintOrder(float forZ)
    {
        var ordered = GetObjectsInOrder(forZ);
        Debug.Log("Bottom → Top: " + string.Join(", ", ordered.Select(o => o.name)));
    }

    public static bool NearlyEqual(float a, float b, float epsilon = 0.4f)
    {
        return Mathf.Abs(a - b) <= epsilon;
    }
}