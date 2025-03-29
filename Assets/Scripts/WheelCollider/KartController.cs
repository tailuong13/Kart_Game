using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class KartController : NetworkBehaviour
{
    private Rigidbody rb;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Wheel Transforms")]
    public Transform frontLeftTransform;
    public Transform frontRightTransform;
    public Transform rearLeftTransform;
    public Transform rearRightTransform;

    [Header("Settings")]
    public float motorTorque = 1500f;
    public float steerAngle = 20f;
    public float brakeForce = 500f;
    public float driftStiffness = 0.6f; // Tăng độ bám đường khi drift
    public float normalStiffness = 1f;

    [Header("Drift Settings")]
    public float driftRecoveryForce = 3f; // Giảm lực kéo ngược lại khi drift
    public float maxDriftAngle = 75f; // Giới hạn góc tối đa khi drift
    public float driftTurnSpeed = 1.5f; // Giảm tốc độ khi đổi hướng drift
    public bool isDrifting = false;
    
    private bool hasInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        StartCoroutine(InitializeKart());
        
        if (IsServer)
        {
            AddToCheckpointSystem();
        }
        else
        {
            RequestAddToCheckpointSystemServerRpc(NetworkObject);
        }
    }
    
    private IEnumerator InitializeKart()
    {
        yield return null;

        if (!hasInitialized)
        {
            hasInitialized = true;
            Debug.Log($"✅ [Client] Player {NetworkManager.LocalClientId} đã spawn và thêm vào checkpoint system!");
        }
    }
    
    private void AddToCheckpointSystem()
    {
        CheckPointsSystem checkpointSystem = FindObjectOfType<CheckPointsSystem>();
        if (checkpointSystem != null)
        {
            checkpointSystem.AddPlayerToCheckpointSystem(NetworkObject);
        }
        else
        {
            Debug.LogError("🚨 Không tìm thấy CheckpointSystem!");
        }
    }
    
    [ServerRpc]
    private void RequestAddToCheckpointSystemServerRpc(NetworkObjectReference carRef)
    {
        if (carRef.TryGet(out NetworkObject carNetworkObject))
        {
            AddToCheckpointSystem();
            SyncCheckpointsClientRpc(carRef);
        }
    }

    [ClientRpc]
    private void SyncCheckpointsClientRpc(NetworkObjectReference carRef)
    {
        if (carRef.TryGet(out NetworkObject carNetworkObject))
        {
            AddToCheckpointSystem();
        }
    }


    [ServerRpc]
    private void SendInputToServerRpc(float accel, float steer, ServerRpcParams rpcParams = default)
    {
        ApplyMotorTorque(accel);
        ApplySteering(steer);
        ApplyBrakes(accel);
        SyncPositionClientRpc(rb.position, rb.rotation);
    }

    [ClientRpc]
    private void SyncPositionClientRpc(Vector3 position, Quaternion rotation)
    {
        if (!IsOwner)
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }
    
    private void FixedUpdate()
    {
        if (!IsOwner) return;

        float accel = Input.GetAxis("Vertical");
        float steer = Input.GetAxis("Horizontal");

        SendInputToServerRpc(accel, steer);
        LimitDriftRotation();
    }

    void ApplyMotorTorque(float accel)
    {
        rearLeftWheel.motorTorque = motorTorque * accel;
        rearRightWheel.motorTorque = motorTorque * accel;
    }

    void ApplySteering(float steer)
    {
        frontLeftWheel.steerAngle = steerAngle * steer;
        frontRightWheel.steerAngle = steerAngle * steer;
    }

    void ApplyBrakes(float accel)
    {
        if (Mathf.Abs(accel) < 0.1f)
        {
            rearLeftWheel.brakeTorque = brakeForce;
            rearRightWheel.brakeTorque = brakeForce;
        }
        else
        {
            rearLeftWheel.brakeTorque = 0;
            rearRightWheel.brakeTorque = 0;
        }
    }

    void StartDrift(float steer)
    {
        if (!isDrifting)
        {
            isDrifting = true;
            rearLeftWheel.sidewaysFriction = AdjustFriction(rearLeftWheel, driftStiffness);
            rearRightWheel.sidewaysFriction = AdjustFriction(rearRightWheel, driftStiffness);
        }
        
        float driftSteerAngle = steerAngle * 0.5f; // Giảm góc đánh lái còn 50%
        frontLeftWheel.steerAngle = (driftSteerAngle / driftTurnSpeed) * steer;
        frontRightWheel.steerAngle = (driftSteerAngle / driftTurnSpeed) * steer;
    }

    void EndDrift()
    {
        if (isDrifting)
        {
            isDrifting = false;
            rearLeftWheel.sidewaysFriction = AdjustFriction(rearLeftWheel, normalStiffness);
            rearRightWheel.sidewaysFriction = AdjustFriction(rearRightWheel, normalStiffness);
        }
    }

    void LimitDriftRotation()
    {
        if (isDrifting)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
            float rotationAngle = Mathf.Atan2(localVelocity.x, localVelocity.z) * Mathf.Rad2Deg;

            // Tính toán góc nghiêng của địa hình
            float slopeAngle = GetGroundSlopeAngle();
            float slopeFactor = Mathf.Clamp(slopeAngle / 45f, 0f, 1f); // Chuẩn hóa góc nghiêng từ 0 đến 1

            // Điều chỉnh lực kéo ngược lại dựa trên góc nghiêng
            Vector3 correctiveForce = -transform.right * Mathf.Sign(rotationAngle) * driftRecoveryForce * (1 - slopeFactor);
            rb.AddForce(correctiveForce, ForceMode.Acceleration);

            // Giới hạn tốc độ xoay của xe
            float maxAngularVelocity = 5f; // Tốc độ xoay tối đa khi drift
            if (rb.angularVelocity.magnitude > maxAngularVelocity)
            {
                rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
            }
        }
    }

    float GetGroundSlopeAngle()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, 1.5f))
        {
            Debug.Log("Ground Slope Angle: ");
            return Vector3.Angle(hit.normal, Vector3.up);
        }
        return 0f;
    }

    WheelFrictionCurve AdjustFriction(WheelCollider wheel, float stiffness)
    {
        WheelFrictionCurve friction = wheel.sidewaysFriction;
        friction.stiffness = stiffness;
        return friction;
    }

    void UpdateWheelPositions()
    {
        UpdateWheelPosition(frontLeftWheel, frontLeftTransform, -90f);
        UpdateWheelPosition(frontRightWheel, frontRightTransform, 90f);
        UpdateWheelPosition(rearLeftWheel, rearLeftTransform, -90f);
        UpdateWheelPosition(rearRightWheel, rearRightTransform, 90f);
    }

    void UpdateWheelPosition(WheelCollider collider, Transform wheelTransform, float yRotation)
    {
        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot * Quaternion.Euler(0, yRotation, 0);
    }
}