using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Serialization;

public class Tower: SnapZone
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
}