using TMPro;
using UnityEngine;

/// <summary>
/// Capa de presentacion editable sin tocar codigo. Cambiar colores, tamanos,
/// fuente y sprites aqui cambia toda la terminal de rol. Si no se asigna uno en
/// escena, se usa una instancia por defecto identica al aspecto original.
/// </summary>
[CreateAssetMenu(menuName = "CyberDefense/Incident UI skin", fileName = "IncidentUISkin")]
public class IncidentUISkin : ScriptableObject
{
    [Header("Colores")]
    public Color panelBackground = new Color(0.03f, 0.06f, 0.09f, 0.97f);
    public Color titleColor = new Color(0.30f, 0.90f, 0.88f);
    public Color bodyColor = Color.white;
    public Color hintColor = new Color(0.70f, 0.80f, 0.90f);
    public Color buttonPrimary = new Color(0.15f, 0.32f, 0.45f);
    public Color buttonSecondary = new Color(0.35f, 0.32f, 0.15f);
    public Color buttonChoice = new Color(0.12f, 0.23f, 0.30f);
    public Color buttonChoiceSelected = new Color(0.16f, 0.42f, 0.50f);
    public Color buttonHint = new Color(0.32f, 0.26f, 0.12f);
    public Color buttonLabelColor = Color.white;
    public Color successColor = new Color(0.40f, 0.90f, 0.50f);
    public Color errorColor = new Color(0.95f, 0.45f, 0.40f);

    [Header("Texto")]
    [Tooltip("Opcional: fuente TMP propia. Vacio = fuente por defecto de TMP.")]
    public TMP_FontAsset font;
    public float titleSize = 30f;
    public float bodySize = 22f;
    public float buttonSize = 18f;
    public float hintSize = 22f;

    [Header("Sprites (opcional)")]
    [Tooltip("Si se asigna, se usa como fondo de botones y paneles.")]
    public Sprite panelSprite;
    public Sprite buttonSprite;

    public static IncidentUISkin CreateDefault()
    {
        var instance = CreateInstance<IncidentUISkin>();
        instance.name = "IncidentUISkin (default)";
        return instance;
    }
}
