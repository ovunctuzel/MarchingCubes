using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPin : MonoBehaviour
{
    public GameObject player;

    void Update()
    {
        transform.position = player.transform.position + Vector3.up * 200;
    }
}
