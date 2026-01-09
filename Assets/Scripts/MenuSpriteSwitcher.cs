using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class MenuSpriteSwitcher : MonoBehaviour
{
    [SerializeField] private Image menuImage;
    [SerializeField] private Sprite coldMenuSprite;
    [SerializeField] private Sprite warmMenuSprite;
    [SerializeField] private bool useWarmVersion = false;

    private void OnEnable() => Apply();
    private void OnValidate() => Apply();

    private void Apply()
    {
        if (menuImage == null) return;
        menuImage.sprite = useWarmVersion ? warmMenuSprite : coldMenuSprite;
        menuImage.preserveAspect = true;
    }
}
