using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class CarSelectionUI : NetworkBehaviour
{
    public Sprite[] carSprites;
    public Image carImage;
    public Button btnLeft, btnRight, btnSelect;

    public Sprite[] characterSprites;      
    public Image characterImage;
    public Button btnCharLeft, btnCharRight;

    public TextMeshProUGUI statusText;

    private int currentCarIndex = 0;
    private int currentCharIndex = 0;

    private bool hasSelected = false;

    private void Start()
    {
        btnLeft.onClick.AddListener(PrevCar);
        btnRight.onClick.AddListener(NextCar);
        btnSelect.onClick.AddListener(SelectCar);

        btnCharLeft.onClick.AddListener(PrevCharacter);
        btnCharRight.onClick.AddListener(NextCharacter);

        UpdateDisplay();
    }

    void PrevCar()
    {
        if (hasSelected) return;
        currentCarIndex = (currentCarIndex - 1 + carSprites.Length) % carSprites.Length;
        UpdateDisplay();
    }

    void NextCar()
    {
        if (hasSelected) return;
        currentCarIndex = (currentCarIndex + 1) % carSprites.Length;
        UpdateDisplay();
    }

    void PrevCharacter()
    {
        if (hasSelected) return;
        currentCharIndex = (currentCharIndex - 1 + characterSprites.Length) % characterSprites.Length;
        UpdateDisplay();
    }

    void NextCharacter()
    {
        if (hasSelected) return;
        currentCharIndex = (currentCharIndex + 1) % characterSprites.Length;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        carImage.sprite = carSprites[currentCarIndex];
        characterImage.sprite = characterSprites[currentCharIndex];
    }

    void SelectCar()
    {
        if (hasSelected) return;

        hasSelected = true;
        statusText.text = $"Đã chọn xe {currentCarIndex + 1} và nhân vật {currentCharIndex + 1}, đợi người khác...";

        SubmitSelectionServerRpc(currentCarIndex, currentCharIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitSelectionServerRpc(int carId, int characterId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        CarSelectionManager.Instance.PlayerSelected(clientId, carId, characterId);
    }
}
