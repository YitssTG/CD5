using Fusion;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AvocadoShark;

/// <summary>
/// Interfaz de seleccion de roles generada por codigo (sin Heat ni prefabs de UI).
///
/// Se coloca en el mismo GameObject que el RoleManager dentro de la escena Game.
/// Mientras la partida no ha comenzado muestra un panel con los dos roles
/// disponibles, el estado del companero y un boton LISTO; bloquea el movimiento
/// del jugador local. Al comenzar, oculta el panel y deja un HUD con el rol.
/// </summary>
public class RoleSelectionUI : MonoBehaviour
{
    [SerializeField] private RoleManager roleManager;

    private GameObject panelRoot;
    private TMP_Text playersLabel;
    private TMP_Text adminLabel;
    private TMP_Text pentesterLabel;
    private TMP_Text messageLabel;
    private TMP_Text errorLabel;
    private Button adminButton;
    private Button pentesterButton;
    private Button readyButton;
    private TMP_Text readyButtonText;
    private TMP_Text hudLabel;

    private bool built;
    private bool? loggedStarted;

    private void Start()
    {
        Build();
    }

    private void Update()
    {
        if (roleManager == null) roleManager = RoleManager.Instance;
        if (!built) return;

        if (roleManager == null || !roleManager.IsOnline)
        {
            panelRoot.SetActive(true);
            hudLabel.gameObject.SetActive(false);
            messageLabel.text = "Conectando con el sistema de roles...";
            adminButton.interactable = pentesterButton.interactable = readyButton.interactable = false;
            SetLocalInput(false);
            return;
        }

        ReconcileLocalRole();
        RefreshLabels();

        bool started = roleManager.GameStarted;
        if (loggedStarted != started)
        {
            loggedStarted = started;
            Debug.Log($"[LocalControl] local={roleManager.LocalPlayerRef} GameStarted={started} adminReady={roleManager.AdminReady} pentesterReady={roleManager.PentesterReady}");
        }
        panelRoot.SetActive(!started);
        hudLabel.gameObject.SetActive(started);

        // Reconcile after avatar registration too, not only on the network transition.
        SetLocalInput(started);

        if (!started)
        {
            SetLocalInput(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Si el jugador pulso un rol antes de que su avatar existiera, aplicamos el
    // rol confirmado en cuanto el avatar local esta disponible.
    private void ReconcileLocalRole()
    {
        if (!roleManager.IsOnline || FusionConnection.Instance == null) return;
        if (!FusionConnection.Instance.TryGetLocalPlayerComponent(out PlayerRole playerRole)) return;

        RoleType assigned = roleManager.LocalRole;
        if (playerRole.MyRole != assigned)
            playerRole.SetRole(assigned);
    }

    private void RefreshLabels()
    {
        if (!roleManager.IsOnline) return;

        PlayerRef local = roleManager.LocalPlayerRef;
        RoleType myRole = roleManager.GetRole(local);

        playersLabel.text = $"Jugadores en sala: {roleManager.ActivePlayerCount}/2";
        adminLabel.text = DescribeSlot(RoleType.Admin, local, roleManager.AdminPlayer, roleManager.AdminReady);
        pentesterLabel.text = DescribeSlot(RoleType.Pentester, local, roleManager.PentesterPlayer, roleManager.PentesterReady);
        messageLabel.text = roleManager.StatusMessage.ToString();
        errorLabel.text = roleManager.LocalErrorMessage ?? string.Empty;

        adminButton.interactable = !IsOccupiedByOther(RoleType.Admin, local) && myRole != RoleType.Admin;
        pentesterButton.interactable = !IsOccupiedByOther(RoleType.Pentester, local) && myRole != RoleType.Pentester;

        bool ready = myRole == RoleType.Admin ? roleManager.AdminReady : roleManager.PentesterReady;
        readyButton.interactable = myRole != RoleType.SinAsignar && !ready;
        readyButtonText.text = myRole == RoleType.SinAsignar
            ? "ELIGE UN ROL"
            : (ready ? "LISTO" : "MARCAR LISTO");

        hudLabel.text = $"ROL: {roleManager.GetRoleName(myRole)}   |   LISTO: {(ready ? "Si" : "No")}";
    }

    private string DescribeSlot(RoleType role, PlayerRef local, PlayerRef assigned, bool ready)
    {
        string name = roleManager.GetRoleName(role);
        if (assigned == PlayerRef.None) return $"{name}: (libre)";
        if (assigned == local) return $"{name}: Tu {(ready ? "- LISTO" : "")}";
        return $"{name}: Companero {(ready ? "- LISTO" : "")}";
    }

    private bool IsOccupiedByOther(RoleType role, PlayerRef local)
    {
        PlayerRef assigned = role == RoleType.Admin ? roleManager.AdminPlayer : roleManager.PentesterPlayer;
        return assigned != PlayerRef.None && assigned != local;
    }

    private void SetLocalInput(bool enabled)
    {
        if (FusionConnection.Instance == null) return;
        if (!FusionConnection.Instance.TryGetLocalPlayerComponent(out StarterAssetsInputs input)) return;

        if (enabled)
        {
            CyberDefense.UI.HeatSessionUI.RefreshGameplayInput();
        }
        else
        {
            input.SetGameplayBlocked(true);
        }
    }

    private void OnAdminClicked()
    {
        if (roleManager == null || !roleManager.IsOnline) return;
        roleManager.ClearLocalError();
        roleManager.RequestRole(RoleType.Admin);
    }

    private void OnPentesterClicked()
    {
        if (roleManager == null || !roleManager.IsOnline) return;
        roleManager.ClearLocalError();
        roleManager.RequestRole(RoleType.Pentester);
    }

    private void OnReadyClicked()
    {
        if (roleManager == null || !roleManager.IsOnline) return;
        roleManager.RequestReady();
    }

    // ------------------------------------------------------------- construccion

    private void Build()
    {
        var canvasObject = new GameObject("Role Selection Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        panelRoot = CreatePanel("Panel", canvasObject.transform, new Vector2(0, 0), new Vector2(1, 1),
            new Color(0.02f, 0.05f, 0.08f, 0.92f), 0);

        var card = CreatePanel("Card", panelRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Color(0.07f, 0.11f, 0.17f, 1f), 0);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(860, 640);

        CreateText("Title", card.transform, "SELECCION DE ROLES", 40,
            new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f), TextAlignmentOptions.Center,
            new Color(0.3f, 0.9f, 0.88f));
        playersLabel = CreateText("Players", card.transform, "Jugadores en sala: 1/2", 22,
            new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.86f), TextAlignmentOptions.Center, Color.white);
        adminLabel = CreateText("AdminSlot", card.transform, "Administrador: (libre)", 24,
            new Vector2(0.1f, 0.66f), new Vector2(0.9f, 0.75f), TextAlignmentOptions.Center,
            new Color(0.55f, 0.85f, 1f));
        pentesterLabel = CreateText("PentesterSlot", card.transform, "Pentester: (libre)", 24,
            new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.66f), TextAlignmentOptions.Center,
            new Color(1f, 0.6f, 0.55f));
        messageLabel = CreateText("Message", card.transform, "", 22,
            new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.57f), TextAlignmentOptions.Center,
            new Color(0.85f, 0.9f, 0.95f));
        errorLabel = CreateText("Error", card.transform, "", 20,
            new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.48f), TextAlignmentOptions.Center,
            new Color(1f, 0.4f, 0.4f));

        adminButton = CreateButton("AdminButton", card.transform, "SER ADMINISTRADOR",
            new Vector2(0.07f, 0.28f), new Vector2(0.48f, 0.40f),
            new Color(0.12f, 0.32f, 0.5f), OnAdminClicked);
        pentesterButton = CreateButton("PentesterButton", card.transform, "SER PENTESTER",
            new Vector2(0.52f, 0.28f), new Vector2(0.93f, 0.40f),
            new Color(0.5f, 0.2f, 0.18f), OnPentesterClicked);
        readyButton = CreateButton("ReadyButton", card.transform, "ELIGE UN ROL",
            new Vector2(0.3f, 0.12f), new Vector2(0.7f, 0.24f),
            new Color(0.15f, 0.45f, 0.3f), OnReadyClicked);
        readyButtonText = readyButton.GetComponentInChildren<TMP_Text>();

        hudLabel = CreateText("RoleHUD", canvasObject.transform, "ROL", 28,
            new Vector2(0.02f, 0.9f), new Vector2(0.5f, 0.98f), TextAlignmentOptions.MidlineLeft,
            new Color(0.98f, 0.78f, 0.25f));
        hudLabel.gameObject.SetActive(false);

        built = true;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Color color, float inset)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static TMP_Text CreateText(string name, Transform parent, string content, float size,
        Vector2 min, Vector2 max, TextAlignmentOptions alignment, Color color)
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
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 min, Vector2 max,
        Color color, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        buttonObject.GetComponent<Image>().color = color;
        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textObject.GetComponent<TMP_Text>();
        text.text = label;
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return button;
    }
}
