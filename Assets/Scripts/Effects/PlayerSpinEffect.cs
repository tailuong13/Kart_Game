using UnityEngine;

public class PlayerSpinEffect : MonoBehaviour
{
    private Rigidbody rb;

    private bool isSpinning = false;
    private float spinTimer = 0f;
    private float spinDuration = 1f;
    private float spinSpeed = 720f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError("❌ Rigidbody của xe không tìm thấy!");
    }

    public void StartSpinning()
    {
        isSpinning = true;
        spinTimer = 0f;
        Debug.Log("✅ Bắt đầu Spin bằng Rigidbody");
        var kart = GetComponent<KartController>();
        if (kart != null)
        {
            kart.isBeingSpin = true;
            Debug.Log("Spin thành công");
        }
        
    }

    private void FixedUpdate()
    {
        if (!isSpinning || rb == null) return;

        float rotationAmount = spinSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);

        spinTimer += Time.fixedDeltaTime;

        if (spinTimer >= spinDuration)
        {
            isSpinning = false;
            Debug.Log("✅ Kết thúc Spin");
            var kart = GetComponent<KartController>();
            if (kart != null)
            {
                kart.isBeingSpin = false;
                Debug.Log("Spin kết thúc, khôi phục trạng thái");
            }
        }
    }
}