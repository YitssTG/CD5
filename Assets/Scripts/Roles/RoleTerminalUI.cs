using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AvocadoShark;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Presentation only: all decisions are validated by CooperativeIncident's state authority.
public class RoleTerminalUI : MonoBehaviour
{
    [SerializeField] private RoleManager roleManager;
    [SerializeField] private IncidentUISkin skin;
    private GameObject panelRoot;
    private TMP_Text titleLabel, bodyLabel, hintLabel;
    private Button primaryButton, secondaryButton, hintButton;
    private readonly Button[] choices = new Button[4];
    private readonly MinigameSelection selection = new MinigameSelection();
    private MinigameDefinition currentMinigame;
    private int shownRevision = -1;
    private bool built, open;
    private static RoleTerminalUI instance;
    private static IncidentUISkin defaultSkin;
    public static bool IsOpen { get; private set; }
    private IncidentUISkin Skin => skin != null ? skin : (defaultSkin != null ? defaultSkin : (defaultSkin = IncidentUISkin.CreateDefault()));
    private void Start() { instance = this; Build(); }

    public static void OpenLocalTerminal()
    {
        if (instance != null && instance.CanUse(out _, out _)) instance.SetOpen(true);
    }

    private bool CanUse(out CooperativeIncident mission, out RoleType role)
    {
        mission = CooperativeIncident.Instance;
        role = RoleType.SinAsignar;
        if (roleManager == null) roleManager = RoleManager.Instance;
        if (roleManager == null || !roleManager.IsOnline || mission == null || !mission.IsOnline ||
            !mission.TeamReady || CyberDefense.UI.HeatSessionUI.IsPaused) return false;
        role = roleManager.LocalRole;
        return mission.IsNear(roleManager.LocalPlayerRef, role, out _);
    }

    private void Update()
    {
        if (!built) return;
        bool usable = CanUse(out var mission, out var role);
        if (!usable) SetOpen(false);
        bool active = mission != null && mission.IsOnline && mission.Definition != null &&
            roleManager != null && roleManager.IsOnline && roleManager.GameStarted;
        hintLabel.gameObject.SetActive(active && !open);
        if (active)
        {
            mission.IsNear(roleManager.LocalPlayerRef, roleManager.LocalRole, out float distance);
            string station = roleManager.LocalRole == RoleType.Pentester ? "servidor de investigacion" : "consola de administracion";
            string objective = mission.TryGetStep(out var step) ? step.objective : StageName(mission.Stage);
            hintLabel.text = $"ALERTA {mission.Definition.incidentId}/{mission.IncidentNumber} | {objective}\n" +
                (float.IsInfinity(distance) ? "No se encontro la terminal en este escenario." :
                $"Acercate al {station}: {distance:0.0} m. " + (usable ? "E / TAB: abrir" : ""));
        }
        if (usable)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) SetOpen(!open);
            if (open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetOpen(false);
#endif
            if (!open && FusionConnection.Instance != null &&
                FusionConnection.Instance.TryGetLocalPlayerComponent(out StarterAssetsInputs input) && input.interact)
            { input.interact = false; SetOpen(true); }
        }
        if (open) RefreshContent();
    }

    private void SetOpen(bool value)
    {
        if (open == value) return;
        open = IsOpen = value;
        panelRoot.SetActive(value);
        if (value) { selection.Clear(); currentMinigame = null; shownRevision = -1; RefreshContent(); }
        CyberDefense.UI.HeatSessionUI.RefreshGameplayInput();
    }
    private void OnDisable() { if (built) SetOpen(false); }
    private void OnDestroy() { if (instance == this) { instance = null; IsOpen = false; } }

    private static string StageName(IncidentStage stage)
    {
        switch(stage)
        {
            case IncidentStage.Alert: return "Investigar alerta";
            case IncidentStage.Investigating: return "Analizar evidencias";
            case IncidentStage.Diagnosed: return "Enviar reporte";
            case IncidentStage.Reported: return "Reporte recibido: contener servicio";
            case IncidentStage.Contained: return "Servicio contenido: elegir reparacion";
            case IncidentStage.Verification: return "Reparacion aplicada: Pentester debe verificar";
            case IncidentStage.Resolved: return "Incidente resuelto";
            default: return "Esperando al equipo";
        }
    }

    private void RefreshContent()
    {
        var m = CooperativeIncident.Instance;
        if (m == null || !m.IsOnline || m.Definition == null) return;
        if (shownRevision != m.Revision) { shownRevision = m.Revision; selection.Clear(); currentMinigame = null; }

        bool pentester = roleManager.LocalRole == RoleType.Pentester;
        var def = m.Definition;
        titleLabel.text = (pentester ? "INVESTIGACION" : "ADMINISTRACION") + $" | {def.incidentId}/{m.IncidentNumber}";

        string text = def.title + "\n";
        text += StageName(m.Stage) + $" | Puntos: {m.Score} | Penalizacion: {m.Penalty}";
        if (m.StepCount > 0 && m.Stage != IncidentStage.Resolved && m.Stage != IncidentStage.Waiting)
            text += $" | Paso {m.CurrentStep + 1}/{m.StepCount}";
        text += "\n";

        bool hasStep = m.TryGetStep(out var step);
        bool authorized = hasStep && roleManager.LocalRole == step.authorizedRole;

        if (m.Stage == IncidentStage.Waiting)
            text += "\nEsperando a que el equipo este listo...";
        else if (m.Stage == IncidentStage.Resolved)
            text += $"\nRESULTADO: {m.Score} puntos a favor, {m.Penalty} de penalizacion, {m.WrongAttempts} fallo(s).\n\n" + def.explanation;
        else if (hasStep)
        {
            text += $"\nOBJETIVO: {step.objective}\n";
            if (authorized)
            {
                if (!string.IsNullOrEmpty(step.briefing)) text += "\n" + step.briefing + "\n";
                if (step.HasMinigame && !string.IsNullOrEmpty(step.minigame.prompt)) text += "\n" + step.minigame.prompt + "\n";
            }
            else
                text += $"\nEsperando a que {(step.authorizedRole == RoleType.Pentester ? "el Pentester" : "el Administrador")} complete: {step.objective}\n";
        }

        string feedback = FeedbackText(m, out _);
        if (!string.IsNullOrEmpty(feedback)) text += "\n" + feedback;

        if (hasStep && step.hints != null)
            for (int i = 0; i < step.hints.Length; i++)
                if (m.IsHintRevealed(i)) text += $"\nPISTA {i + 1}: {step.hints[i]}";

        if (hasStep && authorized && step.HasMinigame && step.minigame.IsSequence && selection.SequenceLength > 0)
            text += "\nSECUENCIA ACTUAL: " + SequenceText(step.minigame);

        float delay = m.RetryAfter.RemainingTime(m.Runner) ?? 0;
        if (delay > 0) text += $"\nReintento disponible en {Mathf.CeilToInt(delay)} s.";
        if (!string.IsNullOrEmpty(m.LocalError)) text += "\n" + m.LocalError;
        bodyLabel.text = text;
        bodyLabel.color = m.Feedback == IncidentFeedback.StepFailed || m.Feedback == IncidentFeedback.NotAllowed
            ? Skin.errorColor
            : m.Feedback == IncidentFeedback.StepComplete ? Skin.successColor : Skin.bodyColor;

        foreach (var b in choices) b.gameObject.SetActive(false);
        primaryButton.gameObject.SetActive(false);
        if (hintButton != null)
        {
            int remaining = hasStep && step.hints != null ? step.hints.Length - CountRevealed(m, step) : 0;
            hintButton.gameObject.SetActive(hasStep && authorized && remaining > 0 &&
                m.Stage != IncidentStage.Resolved && m.Stage != IncidentStage.Waiting);
            if (hintButton.gameObject.activeSelf)
                SetButtonLabel(hintButton, $"Pista (-{m.HintCost})");
        }

        if (hasStep && authorized && m.Stage != IncidentStage.Resolved && m.Stage != IncidentStage.Waiting)
        {
            if (step.HasMinigame && step.minigame.HasOptions)
            {
                currentMinigame = step.minigame;
                Options(step.minigame, selection);
                Action("Confirmar", () =>
                {
                    var mg = currentMinigame;
                    if (mg == null) return;
                    CooperativeIncident.Instance.Submit(selection.ChoiceValue(mg), selection.MaskValue(mg));
                }, selection.CanConfirm(step.minigame));
            }
            else
            {
                currentMinigame = null;
                Action("Continuar", () => CooperativeIncident.Instance.Submit(-1, 0), true);
            }
            primaryButton.interactable &= delay <= 0 && m.TeamReady;
        }
        else
        {
            currentMinigame = null;
        }
    }

    private static int CountRevealed(CooperativeIncident m, IncidentStep step)
    {
        int count = 0;
        if (step.hints == null) return 0;
        for (int i = 0; i < step.hints.Length; i++) if (m.IsHintRevealed(i)) count++;
        return count;
    }

    private static string FeedbackText(CooperativeIncident m, out bool error)
    {
        error = false;
        if (m.Feedback == IncidentFeedback.None || m.LastFeedbackStep < 0) return "";
        if (!m.TryGetStepAt(m.LastFeedbackStep, out var s)) return "";
        if (m.Feedback == IncidentFeedback.StepComplete)
            return string.IsNullOrEmpty(s.successFeedback) ? "Paso completado." : s.successFeedback;
        if (m.Feedback == IncidentFeedback.StepFailed)
        {
            error = true;
            string t = string.IsNullOrEmpty(s.failureFeedback) ? "Intento incorrecto." : s.failureFeedback;
            if (s.minigame != null && !string.IsNullOrEmpty(s.minigame.wrongFeedback))
                t += " " + s.minigame.wrongFeedback;
            return t;
        }
        if (m.Feedback == IncidentFeedback.NotAllowed) { error = true; return "Accion no permitida."; }
        return "";
    }

    private void Options(MinigameDefinition def, MinigameSelection sel)
    {
        var options = def.options;
        int shown = Mathf.Min(options.Length, choices.Length);
        for (int i = 0; i < choices.Length; i++)
        {
            bool active = i < shown;
            choices[i].gameObject.SetActive(active);
            if (!active) continue;
            LayoutOption(i, shown);
            SetButtonLabel(choices[i], OptionLabel(def, sel, i, options[i].label));
            var skin = Skin;
            UIFactory.SetButtonColor(choices[i], sel.IsPicked(i) ? skin.buttonChoiceSelected : skin.buttonChoice);
        }
    }

    private static string OptionLabel(MinigameDefinition def, MinigameSelection sel, int index, string label)
    {
        if (def.IsSequence)
        {
            int position = sel.SequencePosition(index);
            return position > 0 ? $"{position}) {label}" : $"[ ] {label}";
        }
        return (sel.IsPicked(index) ? "[X] " : "[ ] ") + label;
    }

    private string SequenceText(MinigameDefinition def)
    {
        var builder = new System.Text.StringBuilder();
        var options = def.options;
        for (int i = 0; i < selection.SequenceLength; i++)
        {
            int index = selection.SequenceAt(i);
            if (i > 0) builder.Append(" -> ");
            builder.Append(i + 1).Append(") ");
            builder.Append(options != null && index >= 0 && index < options.Length && options[index] != null
                ? options[index].label : "?");
        }
        return builder.ToString();
    }

    private void LayoutOption(int index, int shown)
    {
        const float left = 0.06f, right = 0.94f, gap = 0.012f, bottom = 0.25f, top = 0.36f;
        float width = (right - left - gap * (Mathf.Max(1, shown) - 1)) / Mathf.Max(1, shown);
        float x0 = left + index * (width + gap);
        var rect = choices[index].GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(x0, bottom);
        rect.anchorMax = new Vector2(x0 + width, top);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void Action(string label, UnityEngine.Events.UnityAction callback, bool enabled = true)
    {
        primaryButton.gameObject.SetActive(true);
        primaryButton.interactable = enabled;
        SetButtonLabel(primaryButton, label);
        primaryButton.onClick.RemoveAllListeners();
        primaryButton.onClick.AddListener(callback);
    }

    private static void SetButtonLabel(Button button, string label) { button.GetComponentInChildren<TMP_Text>().text = label; }
    // ------------------------------------------------------------- construccion

    private void Build()
    {
        var canvas = UIFactory.CreateCanvas("Role Terminal Canvas", transform, 250, new Vector2(1920, 1080));
        var canvasTransform = canvas.transform;
        var skin = Skin;

        hintLabel = UIFactory.CreateText("Hint", canvasTransform, "TAB: abrir herramientas de rol", skin.hintSize,
            new Vector2(0.02f, 0.02f), new Vector2(0.95f, 0.12f), TextAlignmentOptions.BottomLeft, skin.hintColor, skin);
        hintLabel.gameObject.SetActive(false);

        panelRoot = UIFactory.CreatePanel("Panel", canvasTransform, new Vector2(0.15f, 0.12f), new Vector2(0.85f, 0.88f),
            skin.panelBackground, 0, skin);

        titleLabel = UIFactory.CreateText("Title", panelRoot.transform, "TERMINAL", skin.titleSize,
            new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f), TextAlignmentOptions.Center, skin.titleColor, skin);
        bodyLabel = UIFactory.CreateText("Body", panelRoot.transform, "", skin.bodySize,
            new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.78f), TextAlignmentOptions.TopLeft, skin.bodyColor, skin);
        hintButton = UIFactory.CreateButton("Hint", panelRoot.transform, "Pista",
            new Vector2(0.72f, 0.80f), new Vector2(0.95f, 0.86f), skin.buttonHint, skin);

        primaryButton = UIFactory.CreateButton("Primary", panelRoot.transform, "Accion (pendiente)",
            new Vector2(0.06f, 0.12f), new Vector2(0.60f, 0.22f), skin.buttonPrimary, skin);
        secondaryButton = UIFactory.CreateButton("Secondary", panelRoot.transform, "Accion (pendiente)",
            new Vector2(0.64f, 0.12f), new Vector2(0.94f, 0.22f), skin.buttonSecondary, skin);

        bodyLabel.enableAutoSizing = true;
        bodyLabel.fontSizeMin = skin.bodySize * 0.72f;
        bodyLabel.fontSizeMax = skin.bodySize;
        bodyLabel.richText = false;
        SetButtonLabel(secondaryButton, "Cerrar (TAB / ESC)");
        secondaryButton.onClick.AddListener(() => SetOpen(false));
        hintButton.onClick.AddListener(() => CooperativeIncident.Instance.RequestHint());
        for (int i = 0; i < choices.Length; i++)
        {
            int index = i;
            choices[i] = UIFactory.CreateButton("Choice" + i, panelRoot.transform, "",
                new Vector2(0.06f + i * 0.22f, 0.25f), new Vector2(0.26f + i * 0.22f, 0.36f), skin.buttonChoice, skin);
            choices[i].onClick.AddListener(() =>
            {
                if (currentMinigame != null) selection.Pick(currentMinigame, index);
                RefreshContent();
            });
        }
        panelRoot.SetActive(false);
        built = true;
    }
}
