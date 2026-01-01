using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessModeRandomEgg : MonoBehaviour
{
    private void Awake()
    {
        int index = Random.Range(0, transform.childCount);
        for (int i = 0; i < transform.childCount; ++i)
        {
            if (i != index) Destroy(transform.GetChild(i).gameObject);
        }
    }
}
