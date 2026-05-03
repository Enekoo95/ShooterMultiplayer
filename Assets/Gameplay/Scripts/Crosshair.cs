using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.1f;

    private float hitTimer = 0f;

    private void Update()
    {
        if (hitTimer > 0f)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
                crosshairImage.color = normalColor;
        }
    }

    // Llama esto cuando impactas a un enemigo
    public void ShowHit()
    {
        hitTimer = hitFlashDuration;
        crosshairImage.color = hitColor;
    }
}