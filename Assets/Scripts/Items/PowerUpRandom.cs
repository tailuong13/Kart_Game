using UnityEngine;
using UnityEngine.UI;

public class PowerUpRandom : MonoBehaviour
{
    public Image powerUpImage;
    
    public Sprite[] powerUpSprites;

    public void RandomPowerUp()
    {
        int randomIndex = Random.Range(0, powerUpSprites.Length);
        powerUpImage.sprite = powerUpSprites[randomIndex];

        Debug.Log($"Random Power-Up: {powerUpSprites[randomIndex].name}");
    }
}