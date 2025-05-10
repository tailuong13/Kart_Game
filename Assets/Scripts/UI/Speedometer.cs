using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class Speedometer : NetworkBehaviour
{
    [Header("References")]
    public Transform targetTransform;
    public RectTransform needleTransform;

    [Header("Settings")]
    public float maxSpeed = 200f;
    public float minNeedleAngle = 123.5f;
    public float maxNeedleAngle = -126.5f;
    public float updateInterval = 0.2f; 
    public float smoothSpeed = 5f; 

    private Vector3 _lastPosition;
    private float _currentSpeed;
    private float _targetAngle;
    private float _currentAngle;

    private void Start()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        if (targetTransform != null)
            _lastPosition = targetTransform.position;

        StartCoroutine(UpdateSpeedCoroutine());
    }

    private void Update()
    {
        if (needleTransform == null) return;
        
        _currentAngle = Mathf.Lerp(_currentAngle, _targetAngle, Time.deltaTime * smoothSpeed);
        needleTransform.localEulerAngles = new Vector3(0, 0, _currentAngle);
    }

    private IEnumerator UpdateSpeedCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            if (targetTransform == null) continue;

            float distance = Vector3.Distance(targetTransform.position, _lastPosition);
            float speed = (distance / updateInterval) * 3.6f; // m/s → km/h
            _currentSpeed = Mathf.Clamp(speed, 0f, maxSpeed);

            float t = _currentSpeed / maxSpeed;
            _targetAngle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, t);

            _lastPosition = targetTransform.position;
        }
    }

    public void SetTarget(Transform tf)
    {
        targetTransform = tf;
    }
}