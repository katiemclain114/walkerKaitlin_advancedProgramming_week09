using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WASDMovement : MonoBehaviour
{
    public float speed = 10f;
    private void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 forward = transform.forward * v * speed * Time.deltaTime;
        Vector3 right = transform.right * h * speed * Time.deltaTime;

        transform.position += (forward + right);

    }
}
