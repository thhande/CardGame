using UnityEngine;
using UnityEngine.UI;

internal static class MatchVisuals
{
    public static readonly Color Ink = new Color32(12, 17, 20, 255);
    public static readonly Color Cream = new Color32(248, 234, 207, 255);
    public static readonly Color Brass = new Color32(214, 174, 107, 255);
    public static readonly Color Rim = new Color32(106, 85, 57, 255);
    public static readonly Color Teal = new Color32(112, 226, 204, 255);
    public static readonly Color Copper = new Color32(255, 155, 111, 255);

    public static void Pin(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    public static void SetPortrait(Image artwork, Sprite sprite)
    {
        artwork.sprite = sprite;
        artwork.enabled = sprite != null;
        if (sprite != null)
            artwork.GetComponent<AspectRatioFitter>().aspectRatio = sprite.rect.width / sprite.rect.height;
    }
}
