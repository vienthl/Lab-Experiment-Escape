using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NeonButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Shadow hoverShadow;
    [SerializeField] private Color idleShadowColor = new(0f, 0.85f, 1f, 0.12f);
    [SerializeField] private Color hoverShadowColor = new(0f, 0.95f, 1f, 0.9f);
    [SerializeField] private float hoverScale = 1.035f;
    [SerializeField] private float animationSpeed = 12f;

    private Vector3 targetScale = Vector3.one;
    private Color targetShadowColor;

    private void Awake()
    {
        if (hoverShadow == null)
        {
            hoverShadow = GetComponent<Shadow>();
        }

        targetShadowColor = idleShadowColor;
        ApplyImmediate();
    }

    private void OnEnable()
    {
        SetHovered(false);
        ApplyImmediate();
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);

        if (hoverShadow != null)
        {
            hoverShadow.effectColor = Color.Lerp(
                hoverShadow.effectColor,
                targetShadowColor,
                Time.unscaledDeltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);

    public void OnPointerExit(PointerEventData eventData) => SetHovered(false);

    public void OnSelect(BaseEventData eventData) => SetHovered(true);

    public void OnDeselect(BaseEventData eventData) => SetHovered(false);

    private void SetHovered(bool hovered)
    {
        targetScale = Vector3.one * (hovered ? hoverScale : 1f);
        targetShadowColor = hovered ? hoverShadowColor : idleShadowColor;
    }

    private void ApplyImmediate()
    {
        transform.localScale = targetScale;

        if (hoverShadow != null)
        {
            hoverShadow.effectColor = targetShadowColor;
        }
    }
}
