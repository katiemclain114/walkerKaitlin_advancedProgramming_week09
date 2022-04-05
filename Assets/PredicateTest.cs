using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredicateTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Vector2[] points = {new Vector2(100, 200), new Vector2(150, 200), new Vector2(200, 200), new Vector2(250, 200)};

        Predicate<Vector2> predicate = FindPoint;

        Vector2 first = Array.Find(points, predicate);
        
        Debug.Log(string.Format("Found: X = {0}, y = {1}", first.x, first.y));
    }

    private static bool FindPoint(Vector2 pos)
    {
        return pos.x + pos.y > 400;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
