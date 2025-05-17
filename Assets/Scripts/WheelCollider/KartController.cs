using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEditor.Rendering;
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

    [Header("Drift Settings")]
    public float driftStiffness = 0.6f;
    public float normalStiffness = 1f;
    public float driftRecoveryForce = 3f;
    public float driftChargeTime = 1.5f;
    public float driftBoostForce = 50f;

    [Header("HUD")]
    [SerializeField] private GameObject canvasHUD;
    public Slider boostBarSlider;
    
    public bool canMove = false;

    // Local input tracking
    private float inputReleaseTimer;
    private float inputReleaseThreshold = 0.5f;
    private bool isDriftInput;
    private bool isDrifting;
    private float driftTimer;
    private float boostTimer;
    private float boostDuration = 1f;
    private bool isBoosting;

    // Synced input values
    private float serverAccel = 0f;
    private float serverSteer = 0f;
    private bool serverBrake = false;
    
    [Header("Power Up Effect")]
    public PowerUpRandom powerUpRandom;

    public bool isBeingSpin = false;
    
    
    [Header("Lap Count")]
    public NetworkVariable<int> lapCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private TextMeshProUGUI lapText;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        StartCoroutine(SetupRigidbody());
        
        if (powerUpRandom == null)
        {
            powerUpRandom = GetComponentInChildren<PowerUpRandom>(true);
        }

        if (IsOwner)
        {
            CheckPointsSystem.Instance.RequestCarListServerRpc();
            Speedometer speedometer = GetComponentInChildren<Speedometer>(true);
            if (speedometer != null)
            {
                speedometer.SetTarget(transform); 
                Debug.Log("✅ Gán targetTransform cho Speedometer");
            }
        }
        else if (canvasHUD != null)
        {
            canvasHUD.SetActive(false);
        }

        rb.centerOfMass = new Vector3(0, -0.7f, 0);

        if (boostBarSlider == null && canvasHUD != null)
        {
            boostBarSlider = canvasHUD.GetComponentInChildren<Slider>(true);
        }

        if (IsServer)
            AddToCheckpointSystem();
        else
            RequestAddToCheckpointSystemServerRpc(NetworkObject);
        
        lapCount.OnValueChanged += OnLapChanged;

        UpdateLapText(lapCount.Value);
    }

    private IEnumerator SetupRigidbody()
    {
        yield return null;
        rb.isKinematic = !(IsOwner || IsServer);
        rb.interpolation = (IsOwner || IsServer)
            ? RigidbodyInterpolation.Interpolate
            : RigidbodyInterpolation.None;
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !canMove) return;
        
        if (IsOwner)
        {
            HandleInput();
            UpdateWheelPositions();
        }

        if (isBeingSpin) return;
        
        if (IsServer)
        {
            ApplyMotorTorque(serverAccel);
            ApplySteering(serverSteer);
            ApplyBrakes(serverBrake);
            SyncPositionClientRpc(rb.position, rb.rotation);
        }

        if (isBoosting)
        {
            boostTimer += Time.fixedDeltaTime;
            if (boostTimer < boostDuration && IsGrounded())
            {
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 velocityXZ = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                if (velocityXZ.magnitude < 25f)
                    rb.AddForce(forward * driftBoostForce, ForceMode.Acceleration);
            }
            else isBoosting = false;
        }
        
        //PowerUp Active
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            switch (powerUpRandom.currentPowerUp)
            {
                case PowerUpRandom.PowerUpType.Banana:
                    Debug.Log("Drop Banana");
                    powerUpRandom.TryDropBanana();
                    break;
                case PowerUpRandom.PowerUpType.Lightning:
                    powerUpRandom.UseLightning();
                    break;
                case PowerUpRandom.PowerUpType.Missile:
                    powerUpRandom.FireMissile();
                    break;
                case PowerUpRandom.PowerUpType.Oil:
                    powerUpRandom.FireOilBullet();
                    break;
                case PowerUpRandom.PowerUpType.Shield:
                    Debug.Log("Active Shield");
                    powerUpRandom.UseShield();
                    break;
                case PowerUpRandom.PowerUpType.Nitro:
                    Debug.Log("Nitro");
                    powerUpRandom.UseNitro();
                    break;
                // ...
            }
            powerUpRandom.ClearPowerUpUI();
        }
    }

    private void HandleInput()
    {
        float accel = Mathf.Abs(Input.GetAxis("Vertical")) > 0.05f ? Input.GetAxis("Vertical") : 0f;
        float steer = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.05f ? Input.GetAxis("Horizontal") : 0f;
        bool brake = Input.GetKey(KeyCode.LeftShift);
        isDriftInput = Input.GetKey(KeyCode.Space);

        if (brake) accel = 0f;

        inputReleaseTimer = (Mathf.Abs(accel) > 0.1f) ? 0f : inputReleaseTimer + Time.fixedDeltaTime;
        if (inputReleaseTimer >= inputReleaseThreshold) accel = 0f;

        HandleDrift(steer);
        UpdateBoostBarUI();
        LimitDriftRotation();

        SendInputToServerRpc(accel, steer, brake);
    }

    private void HandleDrift(float steer)
    {
        if (isDriftInput && Mathf.Abs(steer) > 0.1f)
        {
            if (!isDrifting)
            {
                isDrifting = true;
                driftTimer = 0f;
                StartDriftServerRpc();
            }
            else driftTimer += Time.deltaTime;
        }
        else if (isDrifting)
        {
            isDrifting = false;
           EndDriftServerRpc(driftTimer);
           driftTimer = 0f;
        }
    }
    
    private void LimitDriftRotation()
    {
        if (!isDrifting) return;

        Vector3 localVel = transform.InverseTransformDirection(rb.velocity);
        float angle = Mathf.Atan2(localVel.x, localVel.z) * Mathf.Rad2Deg;
        float slope = GetGroundSlopeAngle();
        float slopeFactor = Mathf.Clamp01(slope / 45f);
        Vector3 correction = -transform.right * Mathf.Sign(angle) * driftRecoveryForce * (1 - slopeFactor);
        rb.AddForce(correction, ForceMode.Acceleration);
    }

    private float GetGroundSlopeAngle()
    {
        return Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 1.5f)
            ? Vector3.Angle(hit.normal, Vector3.up)
            : 0f;
    }

    private void UpdateBoostBarUI()
    {
        if (boostBarSlider == null) return;
        boostBarSlider.maxValue = driftChargeTime;
        boostBarSlider.value = driftTimer;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, -transform.up, 1.5f);
    }

    private void AddToCheckpointSystem()
    {
        CheckPointsSystem system = FindObjectOfType<CheckPointsSystem>();
        system?.AddPlayerToCheckpointSystem(NetworkObject);
    }

    [ServerRpc]
    private void SendInputToServerRpc(float accel, float steer, bool brake)
    {
        serverAccel = accel;
        serverSteer = steer;
        serverBrake = brake;
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
    
    [ServerRpc]
    private void StartDriftServerRpc()
    {
        SetRearFriction(driftStiffness);
    }

    [ServerRpc]
    private void EndDriftServerRpc(float duration)
    {
        SetRearFriction(normalStiffness);
        if (duration >= driftChargeTime)
        {
            isBoosting = true;
            boostTimer = 0f;
        }
    }

    private void SetRearFriction(float stiffness)
    {
        var left = rearLeftWheel.sidewaysFriction;
        var right = rearRightWheel.sidewaysFriction;
        left.stiffness = right.stiffness = stiffness;
        rearLeftWheel.sidewaysFriction = left;
        rearRightWheel.sidewaysFriction = right;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAddToCheckpointSystemServerRpc(NetworkObjectReference carRef)
    {
        if (carRef.TryGet(out NetworkObject obj))
        {
            AddToCheckpointSystem();
            SyncCheckpointsClientRpc(carRef);
        }
    }
    
    [ClientRpc]
    public void RequestRandomPowerUpClientRpc(ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;

        Debug.Log("✅ Client đã nhận được RPC random power up");

        if (powerUpRandom != null)
        {
            powerUpRandom.RandomPowerUp();
        }
        else
        {
            Debug.LogWarning("⚠️ powerUpRandom là null!");
        }
    }

    [ClientRpc]
    private void SyncCheckpointsClientRpc(NetworkObjectReference carRef)
    {
        if (carRef.TryGet(out NetworkObject obj))
        {
            AddToCheckpointSystem();
        }
    }

    private void ApplyMotorTorque(float accel)
    {
        float torque = Mathf.Abs(accel) > 0.05f ? motorTorque * accel : 0f;
        rearLeftWheel.motorTorque = torque;
        rearRightWheel.motorTorque = torque;
    }

    private void ApplySteering(float steer)
    {
        float angle = steerAngle * steer;
        frontLeftWheel.steerAngle = angle;
        frontRightWheel.steerAngle = angle;
    }

    private void ApplyBrakes(bool isBraking)
    {
        float force = (isBraking || Mathf.Abs(serverAccel) < 0.05f) ? brakeForce : 0f;
        rearLeftWheel.brakeTorque = force;
        rearRightWheel.brakeTorque = force;
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
    
    //UI
    private void OnLapChanged(int oldVal, int newVal)
    {
        UpdateLapText(newVal);
    }

    private void UpdateLapText(int val)
    {
        if (lapText != null)
        {
            lapText.text = $"Lap: {val}/2";
        }
    }

}
