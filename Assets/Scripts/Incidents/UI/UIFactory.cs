using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Construccion de UI uGUI por codigo, aplicando un IncidentUISkin. Centraliza
/// el aspecto para no duplicarlo en cada panel.
/// </summary>
public static class UIFactory
{
    public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder, Vector2 referenceResolution)
    {
        var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        return canvas;
    }

    public static GameObject CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Color color, float inset, IncidentUISkin skin)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        var image = panel.GetComponent<Image>();
        image.color = color;
        if (skin != null && skin.panelSprite != null) image.sprite = skin.panelSprite;
        return panel;
    }

    public static TMP_Text CreateText(string name, Transform parent, string content, float size,
        Vector2 min, Vector2 max, TextAlignmentOptions alignment, Color color, IncidentUISkin skin)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        if (skin != null && skin.font != null) text.font = skin.font;
        return text;
    }

    public static Button CreateButton(string name, Transform parent, string label, Vector2 min, Vector2 max, Color color, IncidentUISkin skin)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = buttonObject.GetComponent<Image>();
        image.color = color;
        if (skin != null && skin.buttonSprite != null) image.sprite = skin.buttonSprite;
        var button = buttonObject.GetComponent<Button>();

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textObject.GetComponent<TMP_Text>();
        text.text = label;
        text.fontSize = skin != null ? skin.buttonSize : 18f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = skin != null ? skin.buttonLabelColor : Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        if (skin != null && skin.font != null) text.font = skin.font;
        return button;
    }

    public static void SetButtonColor(Button button, Color color)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image != null) image.color = color;
    }
}
