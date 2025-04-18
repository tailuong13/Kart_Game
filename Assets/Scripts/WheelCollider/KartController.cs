using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UI;

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
    
    [Header("Drift Boost Settings")]
    public float driftChargeTime = 1.5f;
    public float driftBoostForce = 50f;
    private bool isDriftInput = false;
    private float driftTimer = 0f;
    private bool isBoosting = false;
    private float boostDuration = 1f;
    private float boostTimer = 0f;

    private bool hasInitialized = false;
    
    [Header("HUD Settings")]    
    [SerializeField] private GameObject canvasHUD;
    public Slider boostBarSlider;
    
    private float inputReleaseTimer = 0f;
    private float inputReleaseThreshold = 0.5f;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
     
    private void FixedUpdate()
    {
        if (!IsOwner) return;

        bool isBraking = Input.GetKey(KeyCode.LeftShift);
        float accel = Input.GetAxis("Vertical");
        float steer = Input.GetAxis("Horizontal");
        isDriftInput = Input.GetKey(KeyCode.Space);

        if (isBraking) accel = 0;
        
        if (Mathf.Abs(accel) > 0.1f)
        {
            inputReleaseTimer = 0f;
        }
        else
        {
            inputReleaseTimer += Time.fixedDeltaTime;
        }

        if (inputReleaseTimer >= inputReleaseThreshold)
        {
            accel = 0f; 
        }
        
        if (isBoosting)
        {
            boostTimer += Time.fixedDeltaTime;
            if (boostTimer < boostDuration)
            {
                if (IsGrounded())
                {
                    Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                    Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

                    float maxBoostSpeed = 25f;
                    if (horizontalVelocity.magnitude < maxBoostSpeed)
                    {
                        rb.AddForce(flatForward * driftBoostForce, ForceMode.Acceleration);
                    }
                }
            }
            else
            {
                isBoosting = false;
            }
        }
        
        HandleDrift(steer);
        UpdateBoostBarUI();

        SendInputToServerRpc(accel, steer, isBraking);
        LimitDriftRotation();
        
        if (Mathf.Abs(Input.GetAxis("Vertical")) < 0.1f)
        {
            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.fixedDeltaTime * 2f); // tốc độ giảm trớn
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            canvasHUD.SetActive(false);
            return;
        }
        
        if (boostBarSlider == null && canvasHUD != null)
        {
            boostBarSlider = canvasHUD.GetComponentInChildren<Slider>(true);
            if (boostBarSlider != null)
                Debug.Log("✅ BoostBarSlider đã được tìm thấy và gán tự động.");
            else
                Debug.LogWarning("❌ Không tìm thấy BoostBarSlider!");
        }

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
    
    private void HandleDrift(float steer)
    {
        if(isDriftInput && Math.Abs(steer) > 0.1f)
        {
            if (!isDrifting)
            {
                StartDrift(steer);
                driftTimer = 0f;
            }
            else
            {
                driftTimer += Time.deltaTime;
            }
        }
        else
        {
            if (isDrifting)
            {
                EndDrift();
            }
        }
    }
    
    private void UpdateBoostBarUI()
    {
        if (boostBarSlider == null)
        {
            Debug.LogWarning("❌ boostBarSlider chưa được gán trong Inspector!");
            return;
        }

        // boostBarSlider.gameObject.SetActive(isDriftInput || isDrifting);
        boostBarSlider.maxValue = driftChargeTime;
        boostBarSlider.value = driftTimer;
    }

    void ApplyMotorTorque(float accel)
    {
        if (Mathf.Abs(accel) > 0.1f)
        {
            rearLeftWheel.motorTorque = motorTorque * accel;
            rearRightWheel.motorTorque = motorTorque * accel;
        }
        else
        {
            rearLeftWheel.motorTorque = 0f;
            rearRightWheel.motorTorque = 0f;
        }
    }

    void ApplySteering(float steer)
    {
        frontLeftWheel.steerAngle = steerAngle * steer;
        frontRightWheel.steerAngle = steerAngle * steer;
    }

    void ApplyBrakes(bool isBraking)
    {
        if (isBraking)
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
        isDrifting = true;

        rearLeftWheel.sidewaysFriction = AdjustFriction(rearLeftWheel, driftStiffness);
        rearRightWheel.sidewaysFriction = AdjustFriction(rearRightWheel, driftStiffness);

        Debug.Log("🔥 Bắt đầu Drift!");
    }

    void EndDrift()
    {
        isDrifting = false;

        rearLeftWheel.sidewaysFriction = AdjustFriction(rearLeftWheel, normalStiffness);
        rearRightWheel.sidewaysFriction = AdjustFriction(rearRightWheel, normalStiffness);

        if (driftTimer >= driftChargeTime)
        {
            isBoosting = true;
            boostTimer = 0f;
            Debug.Log("💨 Boost Drift được kích hoạt!");
        }
        else
        {
            Debug.Log("⛔ Drift kết thúc mà không đủ thời gian boost.");
        }

        driftTimer = 0f;
        UpdateBoostBarUI();
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

    // void UpdateWheelPositions()
    // {
    //     UpdateWheelPosition(frontLeftWheel, frontLeftTransform, -90f);
    //     UpdateWheelPosition(frontRightWheel, frontRightTransform, 90f);
    //     UpdateWheelPosition(rearLeftWheel, rearLeftTransform, -90f);
    //     UpdateWheelPosition(rearRightWheel, rearRightTransform, 90f);
    // }
    //
    // void UpdateWheelPosition(WheelCollider collider, Transform wheelTransform, float yRotation)
    // {
    //     Vector3 pos;
    //     Quaternion rot;
    //     collider.GetWorldPose(out pos, out rot);
    //     wheelTransform.position = pos;
    //     wheelTransform.rotation = rot * Quaternion.Euler(0, yRotation, 0);
    // }
    
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
    
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 1.5f);
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
    private void SendInputToServerRpc(float accel, float steer, bool isBraking, ServerRpcParams rpcParams = default)
    {
        ApplyMotorTorque(accel);
        ApplySteering(steer);
        ApplyBrakes(isBraking);
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
}