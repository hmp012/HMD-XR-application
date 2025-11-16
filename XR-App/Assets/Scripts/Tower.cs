using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

public class Tower: SnapZone
{
    public DonutsOrder donutsOrder;
    public List<Donut> donutsInTower = new();

    void Awake()
    {
        donutsInTower = gameObject.scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Donut>())
            .Where(o => DonutsOrder.NearlyEqual(transform.position.z, o.transform.position.z))
            .OrderBy(o => o.transform.position.y)
            .ToList();
    }
}