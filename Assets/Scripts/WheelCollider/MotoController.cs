using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Demo Moto Controller (not yet completed)
public class MotoController : MonoBehaviour
{
    private Rigidbody rb;

    public WheelCollider frontWheel;
    public WheelCollider rearWheel;
    public float maxMotorTorque = 2000f;
    public float maxSteerAngle = 20f; // Giới hạn góc rẽ
    public float brakeForce = 500f;
    public float balanceForce = 50f;

    public float maxLeanAngle = 25f;  // Giới hạn góc nghiêng khi rẽ bình thường
    public float driftLeanAngle = 15f; // Giới hạn góc nghiêng khi Drift
    public float leanSpeed = 5f;
    public float maxTurnSpeed = 15f;
    
    private bool isDrifting = false;
    private float currentLean = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
    }

    private void FixedUpdate()
    {
        float accel = Input.GetAxis("Vertical");
        float steer = Input.GetAxis("Horizontal");
        bool driftInput = Input.GetKey(KeyCode.LeftShift);

        if (driftInput && Mathf.Abs(steer) > 0.1f && accel > 0.1f)
        {
            StartDrift(steer);
        }
        else if (isDrifting)
        {
            EndDrift();
        }
        else
        {
            ApplyMotorTorque(accel);
            ApplySteering(steer);
            ApplyBrakes(accel);
            LeanBike(steer, maxLeanAngle);
            StabilizeTurn();
        }
    }

    void ApplyMotorTorque(float accel)
    {
        rearWheel.motorTorque = maxMotorTorque * accel;
    }

    void ApplySteering(float steer)
    {
        frontWheel.steerAngle = maxSteerAngle * steer;
    }

    void ApplyBrakes(float accel)
    {
        if (Mathf.Abs(accel) < 0.1f)
        {
            rearWheel.brakeTorque = brakeForce;
            frontWheel.brakeTorque = brakeForce * 0.5f;
        }
        else
        {
            rearWheel.brakeTorque = 0;
            frontWheel.brakeTorque = 0;
        }
    }

    void LeanBike(float steer, float limitAngle)
    {
        float speedFactor = Mathf.Clamp(rb.velocity.magnitude / maxTurnSpeed, 0, 1);
        float targetLean = -steer * limitAngle * speedFactor;

        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);
        currentLean = Mathf.Clamp(currentLean, -limitAngle, limitAngle);

        Quaternion targetRotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, currentLean);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.deltaTime * 5f));
    }

    void StabilizeTurn()
    {
        if (Mathf.Abs(Input.GetAxis("Horizontal")) < 0.1f)
        {
            rb.angularVelocity *= 0.9f;
            currentLean = Mathf.Lerp(currentLean, 0, Time.deltaTime * 5f);
        }
    }

    void StartDrift(float steer)
    {
        isDrifting = true;

        rb.velocity *= 0.9f; // Giảm tốc độ khi drift
        frontWheel.steerAngle = maxSteerAngle * steer * 0.6f; // Giảm góc cua khi drift
        rearWheel.sidewaysFriction = AdjustFriction(0.4f);

        rb.AddForce(transform.forward * maxMotorTorque * 0.03f, ForceMode.Acceleration);

        // Giới hạn góc nghiêng khi Drift
        LeanBike(steer, driftLeanAngle);
    }

    void EndDrift()
    {
        isDrifting = false;
        StartCoroutine(RestoreGrip());
    }

    IEnumerator RestoreGrip()
    {
        float friction = 0.4f;
        while (friction < 1f)
        {
            friction += 0.05f;
            rearWheel.sidewaysFriction = AdjustFriction(friction);
            yield return new WaitForSeconds(0.1f);
        }
    }

    WheelFrictionCurve AdjustFriction(float stiffness)
    {
        WheelFrictionCurve friction = rearWheel.sidewaysFriction;
        friction.stiffness = stiffness;
        return friction;
    }
}