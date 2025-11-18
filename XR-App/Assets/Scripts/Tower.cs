using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public GameManager gameManager;
    public List<Donut> donutsInTower = new();

    void Awake()
    {
        donutsInTower = gameObject.scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Donut>())
            .Where(o => GameManager.NearlyEqual(transform.position.z, o.transform.position.z))
            .OrderBy(o => o.transform.position.y)
            .ToList();
    }

    private void OnTriggerEnter(Collider other)
    {
        Donut donut = other.GetComponent<Donut>();
        if (donut == null) return;

        Debug.Log("Donut entered tower: " + gameObject.name);

        donutsInTower = gameObject.scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Donut>())
            .Where(o => GameManager.NearlyEqual(transform.position.z, o.transform.position.z))
            .OrderBy(o => o.transform.position.y)
            .ToList();

        if (gameManager != null)
        {
            Debug.Log("Notifying GameManager: OnDonutPlaced()");
            gameManager.OnDonutPlaced();
        }
    }
}