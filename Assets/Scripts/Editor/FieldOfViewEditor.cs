﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (Drones))]
public class FieldOfViewEditor : Editor
{
    private void OnSceneGUI()
    {
        Drones drones = (Drones)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(drones.transform.position, Vector3.up, Vector3.forward, 360, drones.viewRadius);
        Vector3 viewAngleA = drones.GetDirFromAngle(-drones.viewAngle / 2, false);
        Vector3 viewAngleB = drones.GetDirFromAngle(drones.viewAngle / 2, false);

        Handles.DrawLine(drones.transform.position, drones.transform.position + viewAngleA * drones.viewRadius);
        Handles.DrawLine(drones.transform.position, drones.transform.position + viewAngleB * drones.viewRadius);

        Handles.color = Color.red;
        foreach(Transform visibleTarget in drones.targetsInView)
        {
            Handles.DrawLine(drones.transform.position, visibleTarget.position);
        }
    }
}
