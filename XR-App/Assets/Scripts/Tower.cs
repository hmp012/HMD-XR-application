using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Tower : MonoBehaviour
{
    private void Reset()
    {
       
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    
}