using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class CarSelectionUI : NetworkBehaviour
{
    public Sprite[] carSprites; 
    public Image carImage;      
    public Button btnLeft, btnRight, btnSelect;
    public TextMeshProUGUI statusText;

    private int currentIndex = 0;
    private bool hasSelected = false;

    private void Start()
    {
        btnLeft.onClick.AddListener(PrevCar);
        btnRight.onClick.AddListener(NextCar);
        btnSelect.onClick.AddListener(SelectCar);

        UpdateDisplay();
    }

    void PrevCar()
    {
        if (hasSelected) return;
        currentIndex = (currentIndex - 1 + carSprites.Length) % carSprites.Length;
        UpdateDisplay();
    }

    void NextCar()
    {
        if (hasSelected) return;
        currentIndex = (currentIndex + 1) % carSprites.Length;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        carImage.sprite = carSprites[currentIndex];
    }

    void SelectCar()
    {
        if (hasSelected) return;

        hasSelected = true;
        statusText.text = $"Đã chọn xe {currentIndex + 1}, đợi người khác...";
        
        SubmitCarSelectionServerRpc(currentIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitCarSelectionServerRpc(int carId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        CarSelectionManager.Instance.PlayerSelectedCar(clientId, carId);
    }
}
