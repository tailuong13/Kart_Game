using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Speedometer : MonoBehaviour
{
    [Header("References")]
    public Rigidbody targetRigidbody;
    public RectTransform needleTransform;

    [Header("Settings")]
    public float maxSpeed = 200f; 
    private float _minNeedleAngle;
    public float maxNeedleAngle = -126.5f;  
    
    private void Start()
    {
        _minNeedleAngle = needleTransform.localEulerAngles.z;
        maxNeedleAngle = -126.5f;
        maxSpeed = 200f; 
    }
    
    private void Update()
    {
        if (targetRigidbody == null || needleTransform == null) return;

        float speed = targetRigidbody.velocity.magnitude * 3.6f;
        float clampedSpeed = Mathf.Min(speed, maxSpeed);
        float t = clampedSpeed / maxSpeed;
        float angle = Mathf.Lerp(_minNeedleAngle, maxNeedleAngle, t);

        needleTransform.localEulerAngles = new Vector3(0, 0, angle);
        Debug.Log($"🚗 Speed: {speed:F1} km/h | Angle: {angle:F2}");
    }
}