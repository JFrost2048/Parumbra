using System.Collections.Generic;
using UnityEngine;

public class Obj_Toggle : MonoBehaviour
{
    [SerializeField] private List<GameObject> targets = new List<GameObject>();

    public void Toggle()
    {
        foreach (var t in targets)
        {
            if (t == null) continue;
            t.SetActive(!t.activeSelf);
        }
    }
}
