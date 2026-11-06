using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using AvocadoShark;
using StarterAssets;

// Local tablet/PC view. No UI callback can mutate mission state directly.
public class RoleTerminalUI : MonoBehaviour
{
    [SerializeField] private RoleManager roleManager;
    [SerializeField] private IncidentUISkin skin;
    private static RoleTerminalUI instance;
    public static bool TabletVisible => instance != null && instance.visible && !instance.pc;
    public static bool TabletInteractive => TabletVisible && instance.pointer;
    public static bool IsOpen => instance != null && instance.visible && instance.pointer;
    public static bool IsTyping => EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
        EventSystem.current.currentSelectedGameObject.TryGetComponent<TMP_InputField>(out var field) && field.isFocused;
    private GameObject panel, form, bodyViewport;
    private GameObject hudPanel;
    private CanvasGroup gameCanvasGroup;
    private bool gameCanvasHidden;
    private float savedCanvasAlpha;
    private bool savedCanvasInteractable, savedCanvasRaycasts;
    private RectTransform panelRect;
    private Transform overlayRoot;
    private bool showForm, showIdentifier;
    private readonly System.Collections.Generic.HashSet<GameObject> visibleControls = new System.Collections.Generic.HashSet<GameObject>();
    private CanvasGroup group;
    private TMP_Text title, body, hud, toast, error, progressLabel;
    private TMP_Text controls;
    private Image progress;
    private TMP_InputField identifier, comment;
    private Button primary, secondary, hint;
    private Button[] options = new Button[4], fields = new Button[4], evidence = new Button[4];
    private Button[] tabs = new Button[4];
    private int displayedPage = -1;
    private MinigameSelection selection = new MinigameSelection();
    private int page, revision = -1, currentIncident = -1, step = -1, diagnosis = -1, severity = -1, recommendation = -1, system = -1, evidenceMask;
    private bool visible, pointer, pc;
    private float blend, toastUntil;
    private int tutorialPage;
    private CooperativeIncident subscribed;
    private MissionPresentationConfig config;
    private IncidentUISkin currentSkin;
    private AudioSource sound;
    private Action primaryAction, secondaryAction;
    private string localNotice;
    private static readonly string[] evidenceLabels = { "Entrada concatenada en SQL", "Condicion de consulta alterada", "Acceso sin sesion valida", "Correo de phishing no observado" };

    private void Start()
    {
        instance = this; config = MissionPresentationConfig.Load();
        currentSkin = skin != null ? skin : config.pentesterSkin != null ? config.pentesterSkin : IncidentUISkin.CreateDefault();
        Build(); sound = gameObject.AddComponent<AudioSource>(); sound.playOnAwake = false; sound.spatialBlend = 0;
        gameObject.AddComponent<MissionWorldPresentation>();
    }
    private RoleType MyRole => roleManager != null && roleManager.IsOnline ? roleManager.LocalRole : RoleType.SinAsignar;
    private CooperativeIncident Mission => CooperativeIncident.Instance;
    public static void OpenLocalTerminal()
    {
        if (instance == null || instance.Mission == null || !instance.Mission.IsOnline || IsTyping || CyberDefense.UI.HeatSessionUI.IsPaused) return;
        if (instance.Mission.IsNear(instance.Mission.Runner.LocalPlayer, instance.MyRole, out _))
        { instance.pc = true; instance.page = instance.Mission.Phase == MissionPhase.Tutorial ? 3 : instance.MyRole == RoleType.Admin ? 1 : 0; instance.Open(true); }
    }
    public static bool ConsumeEscape()
    {
        if (IsTyping)
        {
            var selected = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
            selected.DeactivateInputField(); EventSystem.current.SetSelectedGameObject(null); return true;
        }
        if (instance == null || !instance.visible) return false;
        if (instance.pointer && !instance.pc) instance.pointer = false; else instance.Open(false);
        CyberDefense.UI.HeatSessionUI.RefreshGameplayInput(); return true;
    }
    private void Open(bool value)
    {
        visible = value; pointer = value;
        if (!value) { pc = false; if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); }
        if (value) { localNotice = null; if (config.click != null) sound.PlayOneShot(config.click, .3f); }
        PublishTablet(); CyberDefense.UI.HeatSessionUI.RefreshGameplayInput();
    }
    private void PublishTablet()
    {
        if (FusionConnection.Instance != null && FusionConnection.Instance.TryGetLocalPlayerComponent(out PlayerRole p)) p.TabletRaised = visible && !pc;
    }
    private void OnDisable() { if (panel != null) Open(false); SetGameCanvasHidden(false); }
    private void OnDestroy()
    {
        SetGameCanvasHidden(false);
        if (subscribed != null) subscribed.EventPresented -= OnEvent;
        if (panel != null) Destroy(panel);
        if (instance == this) instance = null;
    }
    private void OnEvent(MissionRecord record)
    {
        toast.text = record.Detail.ToString(); toastUntil = Time.unscaledTime + 5;
        if (config.notification != null) sound.PlayOneShot(config.notification, .3f);
    }
    private void Update()
    {
        if (panel == null) return;
        if (roleManager == null) roleManager = RoleManager.Instance;
        var m = Mission;
        if (m == null || !m.IsOnline || MyRole == RoleType.SinAsignar) { Open(false); hud.text = "Conectando con el equipo..."; Animate(); return; }
        if (subscribed != m)
        { if (subscribed != null) subscribed.EventPresented -= OnEvent; subscribed = m; subscribed.EventPresented += OnEvent; }
        if (currentIncident != m.IncidentNumber)
        { currentIncident = m.IncidentNumber; page = 3; tutorialPage = 0; evidenceMask = 0; diagnosis = severity = recommendation = system = -1; comment.text = ""; identifier.text = ""; localNotice = null; }
        if (step != m.CurrentStep) { step = m.CurrentStep; selection.Clear(); }
        revision = m.Revision;
        bool paused = CyberDefense.UI.HeatSessionUI.IsPaused;
        if (!paused && !IsTyping && Keyboard.current != null)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame) { pc = false; if (!visible) { Open(true); } else Open(false); }
            if (visible && !pc && Keyboard.current.f1Key.wasPressedThisFrame) { pointer = !pointer; if (!pointer && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); }
        }
        if (!paused && (!visible || !pointer) && !IsTyping && FusionConnection.Instance != null &&
            FusionConnection.Instance.TryGetLocalPlayerComponent(out StarterAssetsInputs input) && input.interact)
        { input.interact = false; OpenLocalTerminal(); }
        if (pc && !m.IsNear(m.Runner.LocalPlayer, MyRole, out _)) { Open(false); localNotice = "Te alejaste de la estacion."; }
        if (m.Finished && !visible) { pc = false; page = 2; Open(true); }
        PublishTablet(); Animate();
        var nextSkin = MyRole == RoleType.Admin ? config.adminSkin : config.pentesterSkin;
        if (nextSkin != null && nextSkin != currentSkin)
        {
            currentSkin = nextSkin; panel.GetComponent<Image>().color = currentSkin.panelBackground;
            title.color = progress.color = currentSkin.titleColor;
            foreach (var b in panel.GetComponentsInChildren<Button>(true)) UIFactory.SetButtonColor(b, currentSkin.buttonPrimary);
        }
        string objective = m.TryGetStep(out var active) ? active.objective : "Mision resuelta";
        string clock = TimeSpan.FromSeconds(m.Elapsed).ToString(@"mm\:ss");
        m.IsNear(m.Runner.LocalPlayer, MyRole, out float distance);
        string location = float.IsInfinity(distance) ? "Estacion no disponible" : $"{(MyRole == RoleType.Pentester ? m.Definition.serverId : "PC ADMIN")} a {distance:0.0} m";
        hud.text = $"{(MyRole == RoleType.Admin ? "ADMINISTRADOR" : "PENTESTER")}  ·  {clock}  ·  {m.NetScore} pts\n" +
            (m.Phase == MissionPhase.Tutorial ? "Explora. Abre la tablet para comenzar." : objective);
        int unread = ChatSystem.Instance != null ? ChatSystem.Instance.Unread : 0;
        controls.text = "TAB Tablet   ·   F2 Chat" + (unread > 0 ? $" ({unread})" : "") + "   ·   F3 Equipo   ·   ESC Menu" +
            (distance <= m.Definition.interactionRadius ? "\nE · Interactuar con tu estacion" : "");
        if (FusionConnection.Instance != null && FusionConnection.Instance.TryGetLocalPlayerComponent(out StarterAssetsInputs voiceInput) && voiceInput.pushToTalk) hud.text += "   |   MIC ACTIVO";
        toast.gameObject.SetActive(!visible && Time.unscaledTime < toastUntil);
        if (visible) Refresh();
        if (m.Phase != MissionPhase.Waiting) MissionExitReport.Capture(m);
    }
    private void Animate()
    {
        Canvas tablet = !pc ? TabletPresentation.LocalScreen : null;
        Transform destination = tablet != null ? tablet.transform : overlayRoot;
        if (panelRect.parent != destination)
        {
            panelRect.SetParent(destination, false);
            panelRect.localRotation = Quaternion.identity; panelRect.localScale = Vector3.one;
        }
        blend = Mathf.MoveTowards(blend, visible ? 1 : 0, Time.unscaledDeltaTime / Mathf.Max(.1f, config.transitionSeconds));
        SetGameCanvasHidden(visible || blend > .001f);
        if (hudPanel != null) hudPanel.SetActive(!visible && blend <= .001f && !CyberDefense.UI.HeatSessionUI.IsPaused);
        if (controls != null) controls.gameObject.SetActive(!visible && blend <= .001f && !CyberDefense.UI.HeatSessionUI.IsPaused);
        if (toast != null && visible) toast.gameObject.SetActive(false);
        panel.SetActive(blend > .001f); group.alpha = blend;
        group.interactable = group.blocksRaycasts = visible && pointer && !CyberDefense.UI.HeatSessionUI.IsPaused;
        if (tablet != null)
        {
            tablet.worldCamera = Camera.main;
            panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            return;
        }
        Vector2 min = pointer || pc ? new Vector2(.13f,.12f) : new Vector2(.50f,.03f);
        Vector2 max = pointer || pc ? new Vector2(.87f,.88f) : new Vector2(.98f,.59f);
        float smooth = 1-Mathf.Exp(-14*Time.unscaledDeltaTime);
        panelRect.anchorMin = Vector2.Lerp(panelRect.anchorMin, min + Vector2.down*(1-blend), smooth);
        panelRect.anchorMax = Vector2.Lerp(panelRect.anchorMax, max + Vector2.down*(1-blend), smooth);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
    }
    private void Refresh()
    {
        var m = Mission;
        title.text = (pc ? "ESTACION / " : "TABLET / ") + (MyRole == RoleType.Admin ? "ADMINISTRACION" : "INVESTIGACION") + $"  •  {(m.IsPhishing ? "PHISHING" : m.Definition.incidentId)}";
        showForm = showIdentifier = false;
        visibleControls.Clear();
        primaryAction = secondaryAction = null;
        float remaining = m.RetryAfter.RemainingTime(m.Runner) ?? 0;
        error.text = localNotice ?? m.LocalError ?? "";
        if (remaining > 0) error.text += $"\nReintento en {Mathf.CeilToInt(remaining)} s.";
        progress.transform.parent.gameObject.SetActive(m.Operating);
        progress.rectTransform.anchorMax = new Vector2(m.Progress,1);
        progressLabel.text = m.Operating ? $"Operacion simulada {m.Progress:P0} • permanece junto a tu estacion" : "";
        if (page == 3 && !m.Finished) Tutorial(m);
        else if (page == 2 || m.Finished) History(m);
        else if (page == 1) Tickets(m);
        else Overview(m);
        // Apply visibility once. Disabling and re-enabling a TMP input every frame
        // deactivates editing and clears EventSystem focus on every keystroke.
        SetVisible(form, showForm); SetVisible(bodyViewport, !showForm);
        SetVisible(identifier.gameObject, showIdentifier);
        bool canAct = !m.Operating && remaining <= 0;
        if (m.Phase == MissionPhase.Active) { primary.interactable &= canAct; secondary.interactable &= canAct; }
        if (m.Phase == MissionPhase.Active && m.TryGetStep(out var stepData) && stepData.authorizedRole == MyRole && stepData.hints != null && stepData.hints.Length > 0)
        { visibleControls.Add(hint.gameObject); hint.interactable = canAct; Label(hint, $"Pista (-{m.HintCost} pts)");
            for(int i=0;i<stepData.hints.Length && i<30;i++) if(m.IsHintRevealed(i))
            { if(form.activeSelf) error.text += "\nPISTA: " + stepData.hints[i]; else body.text += "\nPISTA: " + stepData.hints[i]; } }
        foreach (var b in options) SetVisible(b.gameObject, visibleControls.Contains(b.gameObject));
        foreach (var b in new[] { primary, secondary, hint }) SetVisible(b.gameObject, visibleControls.Contains(b.gameObject));
        bool hasOptions = false;
        foreach (var b in options) hasOptions |= b.gameObject.activeSelf;
        var viewport = bodyViewport.GetComponent<RectTransform>();
        viewport.anchorMax = new Vector2(hasOptions ? .62f : .96f, .83f);
        for (int i = 0; i < tabs.Length; i++)
            UIFactory.SetButtonColor(tabs[i], i == page ? currentSkin.titleColor * .65f : currentSkin.buttonPrimary);
        if (displayedPage != page)
        {
            bodyViewport.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
            displayedPage = page;
        }
    }
    private void Tutorial(CooperativeIncident m)
    {
        string[] pages = m.IsPhishing ? new[] {
            "SEGUNDO INCIDENTE: PHISHING\nEste ejercicio es simulado. No abras enlaces reales ni introduzcas contrasenas.",
            "PENTESTER / ATACANTE AUTORIZADO\nAnaliza remitente, urgencia y dominio. Comunica al defensor un diagnostico basado en lo observado.",
            "DEFENSOR\nContiene el riesgo, revoca sesiones y recupera el acceso por un canal confiable. El pentester comprueba la respuesta." }
            : MyRole == RoleType.Pentester ? new[] {
            "EXPLORACION LIBRE\nWASD: caminar. Raton: mirar. Espacio: saltar. Shift: correr sin tablet.\nTAB saca/guarda tablet. Con ella puedes caminar mas despacio. F1 activa/desactiva el cursor. Al escribir se bloquea movimiento. ESC vuelve un nivel.",
            "INVESTIGACION\nBusca la etiqueta SRV-01. Introduce ese ID en la tablet cerca del servidor y analiza. E abre la estacion. Selecciona las evidencias de la consulta y el acceso anomalo.",
            "COOPERACION\nRedacta el ticket en tu tablet: servicio, diagnostico, evidencias, gravedad y recomendacion. El Administrador lo revisa. Puedes corregirlo si lo devuelve. Tu verificas la reparacion final. No puedes reparar." }
            : new[] {
            "EXPLORACION LIBRE\nWASD y raton para explorar. TAB tablet, F1 cursor y ESC volver. E abre tu PC al acercarte. El chat y la voz siguen disponibles. El incidente aun no consume tiempo de mision.",
            "REVISION\nTu tablet recibe un resumen del ticket. Introduce su codigo en la PC ADMIN para abrir el informe completo. Contrasta las evidencias; acepta o solicita una correccion.",
            "RESPUESTA\nCon reporte aceptado, identifica el servicio, contienelo y ordena la reparacion. El Pentester debe verificar antes de restaurar. Las decisiones incorrectas restan puntos. El reloj no se pausa al abrir interfaces." };
        body.text = $"TUTORIAL {tutorialPage+1}/{pages.Length}\n\n{pages[tutorialPage]}\n\n" +
            $"Preparados: Pentester {(m.PentesterReady ? "SI" : "NO")} / Administrador {(m.AdminReady ? "SI" : "NO")}";
        if (m.Phase != MissionPhase.Tutorial) { body.text += "\nEl tutorial esta disponible como referencia."; return; }
        bool ready = MyRole == RoleType.Pentester ? m.PentesterReady : m.AdminReady;
        if (ready) { body.text += "\nEsperando confirmacion del companero. Puedes guardar la tablet y explorar."; return; }
        if (tutorialPage < 2) Primary("Siguiente", () => tutorialPage++);
        else Primary("Tutorial completado: estoy preparado", () => m.ConfirmTutorial(false));
        Secondary("Omitir tutorial y confirmar preparado", () => m.ConfirmTutorial(true));
    }
    private void Overview(CooperativeIncident m)
    {
        if (m.Phase == MissionPhase.Tutorial) { body.text = "Explora antes de iniciar. Abre la pestana TUTORIAL y confirma preparado cuando termines."; return; }
        if (!m.TryGetStep(out var stepData)) { body.text = "Esperando al equipo..."; return; }
        bool authorized = MyRole == stepData.authorizedRole;
        body.text = $"PASO {m.CurrentStep+1}/{m.StepCount}\n{stepData.objective}\n\n";
        if (!authorized) { body.text += $"Tu companero ({stepData.authorizedRole}) esta trabajando en este paso.\nPuedes consultar tu tablet y comunicarte por chat/voz."; return; }
        if (!m.IsPhishing && (m.CurrentStep == 2 || m.CurrentStep == 3)) { body.text += "Continua desde la pestana TICKETS."; Primary("Abrir tickets", () => page = 1); return; }
        if (!m.IsPhishing && MyRole == RoleType.Pentester && !m.ServerIdentified)
        {
            body.text += "Introduce el ID que aparece en la etiqueta del servidor. La identificacion y el analisis requieren proximidad.\nNo hay penalizacion por escribir mal el ID.";
            showIdentifier = true; Placeholder(identifier, "ID del servidor, por ejemplo SRV-01");
            Primary("Conectar al servidor", () => m.Identify(identifier.text)); return;
        }
        if (!m.IsPhishing && m.CurrentStep == 1 && !pc) { body.text += "Analisis listo. Acercate al servidor y pulsa E para abrir sus registros en la estacion."; return; }
        body.text += stepData.briefing;
        if (stepData.HasMinigame)
        {
            body.text += "\n\n" + stepData.minigame.prompt;
            for (int i = 0; i < options.Length && i < stepData.minigame.options.Length; i++)
            {
                int index = i; var def = stepData.minigame; var b = options[i]; visibleControls.Add(b.gameObject);
                string prefix = def.IsSequence && selection.SequencePosition(i)>0 ? selection.SequencePosition(i)+". " : selection.IsPicked(i) ? "[X] " : "[ ] ";
                Label(b, prefix + def.options[i].label); b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => selection.Pick(def,index));
            }
            Primary("Confirmar decision", () => m.Submit(selection.ChoiceValue(stepData.minigame), selection.MaskValue(stepData.minigame)), selection.CanConfirm(stepData.minigame));
        }
        else Primary("Iniciar analisis", () => m.Submit(-1));
        if (MyRole == RoleType.Admin && !pc) { primary.interactable = false; body.text += "\n\nLas operaciones administrativas se realizan en tu PC (E)."; }
        if (!m.IsNear(m.Runner.LocalPlayer, MyRole, out _)) { primary.interactable = false; body.text += "\n\nAcercate a tu estacion para ejecutar esta accion."; }
    }
    private void Tickets(CooperativeIncident m)
    {
        if (m.IsPhishing) { body.text = "En este ejercicio el reporte se comunica mediante la decision de la pestana OPERACIONES."; return; }
        if (m.Phase != MissionPhase.Active) { body.text = "Los tickets se habilitan durante el incidente."; return; }
        if (MyRole == RoleType.Pentester && m.CurrentStep == 2)
        {
            showForm = true;
            Label(fields[0], "Diagnostico: " + (diagnosis < 0 ? "elige una opcion" : TicketRules.DiagnosisName(diagnosis)));
            Label(fields[1], "Gravedad: " + (severity < 0 ? "elige una opcion" : TicketRules.SeverityName(severity)));
            Label(fields[2], "Sugerencia: " + (recommendation < 0 ? "elige una opcion" : TicketRules.RecommendationName(recommendation)));
            Label(fields[3], "Servicio: " + (system < 0 ? "elige una opcion" : system == 0 ? "Portal de matriculas" : "Correo"));
            for(int i=0;i<4;i++) Label(evidence[i], ((evidenceMask & 1<<i)!=0 ? "[X] " : "[ ] ")+evidenceLabels[i]);
            Primary("Enviar ticket", () => m.SendTicket(diagnosis,evidenceMask,severity,recommendation,system,comment.text), TicketRules.IsValid(diagnosis,evidenceMask,severity,recommendation,system));
            if(m.TicketState == TicketStatus.Returned) error.text = "Ticket devuelto: corrige sus campos y vuelve a enviarlo.";
            return;
        }
        if (m.Ticket.Version == 0) { body.text = "No hay tickets enviados. El Pentester debe analizar las evidencias y redactar el reporte."; return; }
        var t = m.Ticket; string code = TicketRules.Code(m.IncidentNumber,t.Version);
        body.text = $"TICKET {code}\nServidor: {m.Definition.serverId}\nGravedad indicada: {TicketRules.SeverityName(t.Severity)}\nEstado: {TicketStateName(m.TicketState)}\n";
        if (MyRole == RoleType.Admin && !pc)
        { body.text += "\nIntroduce este codigo en PC ADMIN para consultar el analisis completo.\nE cerca de la estacion abre la PC."; return; }
        if (MyRole == RoleType.Admin && m.TicketUnlockedVersion != t.Version)
        {
            showIdentifier = true; Placeholder(identifier, "Codigo del ticket");
            Primary("Abrir reporte completo", () => m.UnlockTicket(identifier.text)); return;
        }
        body.text += $"\nDiagnostico: {TicketRules.DiagnosisName(t.Diagnosis)}\nServicio: {(t.System==0 ? "Portal de matriculas" : "Correo")}\n" +
            $"Sugerencia: {TicketRules.RecommendationName(t.Recommendation)}\nComentario: {t.Comment}\n\nEvidencias marcadas:\n";
        for(int i=0;i<4;i++) if((t.EvidenceMask & 1<<i)!=0) body.text += "• " + evidenceLabels[i]+"\n";
        body.text += "\nREGISTROS ADJUNTOS\n" + m.Definition.evidence;
        if(MyRole == RoleType.Admin && m.CurrentStep==3 && m.TicketState==TicketStatus.Submitted)
        { Primary("Aceptar reporte", () => m.ReviewTicket(true)); Secondary("Solicitar correccion", () => m.ReviewTicket(false)); }
        else if(MyRole == RoleType.Admin && m.TicketState==TicketStatus.Accepted) Primary("Abrir operaciones",()=>page=0);
    }
    private void History(CooperativeIncident m)
    {
        body.text = (m.Finished ? m.Summary() : $"Tiempo de mision: {TimeSpan.FromSeconds(m.Elapsed):mm\\:ss}\nPuntos: {m.NetScore}\n") + "\n\nCRONOLOGIA\n" + m.Journal();
        if (m.Finished)
        {
            Primary("Volver al menu", () => { MissionExitReport.Dismiss(); FusionConnection.Instance.ReturnToMenu(); });
            if (m.Phase == MissionPhase.Complete && m.TeamReady)
            {
                Secondary("Repetir (ambos deben confirmar)", () => m.RequestReplay(false));
                visibleControls.Add(options[0].gameObject); Label(options[0], "Intercambiar roles y repetir"); options[0].onClick.RemoveAllListeners(); options[0].onClick.AddListener(()=>m.RequestReplay(true));
                if (!m.IsPhishing) { visibleControls.Add(options[1].gameObject); Label(options[1], "Continuar a phishing (ambos)"); options[1].onClick.RemoveAllListeners(); options[1].onClick.AddListener(()=>m.RequestPhishing()); }
                body.text += $"\nVotos: Admin {m.AdminReplay} / Pentester {m.PentesterReplay} (1 repetir, 2 intercambiar).";
            }
        }
    }
    private void Primary(string label, Action action, bool enabled=true) { visibleControls.Add(primary.gameObject); Label(primary,label); primary.interactable=enabled; primaryAction=action; }
    private void Secondary(string label, Action action) { visibleControls.Add(secondary.gameObject); Label(secondary,label); secondary.interactable=true; secondaryAction=action; }
    private static void Label(Button b,string text) { b.GetComponentInChildren<TMP_Text>().text=text; }
    private static string PhaseName(MissionPhase p) => p == MissionPhase.Active ? "MISION ACTIVA" : p == MissionPhase.Complete ? "RESUELTO" : p == MissionPhase.Interrupted ? "INTERRUMPIDA" : p == MissionPhase.Tutorial ? "TUTORIAL" : "ESPERANDO";
    private static string TicketStateName(TicketStatus s) => s == TicketStatus.Submitted ? "En revision" : s == TicketStatus.Returned ? "Devuelto" : s == TicketStatus.Accepted ? "Aceptado" : "Sin ticket";
    private static void Placeholder(TMP_InputField field,string text) { ((TMP_Text)field.placeholder).text=text; }
    private static void SetVisible(GameObject target, bool value) { if (target.activeSelf != value) target.SetActive(value); }
    private void SetGameCanvasHidden(bool hidden)
    {
        if (gameCanvasGroup == null && hidden)
        {
            var legacy = GameObject.Find("GameCanvas");
            if (legacy == null) return;
            gameCanvasGroup = legacy.GetComponent<CanvasGroup>();
            if (gameCanvasGroup == null) gameCanvasGroup = legacy.AddComponent<CanvasGroup>();
        }
        if (gameCanvasGroup == null || hidden == gameCanvasHidden) return;
        if (hidden)
        {
            savedCanvasAlpha = gameCanvasGroup.alpha; savedCanvasInteractable = gameCanvasGroup.interactable; savedCanvasRaycasts = gameCanvasGroup.blocksRaycasts;
            gameCanvasGroup.alpha = 0; gameCanvasGroup.interactable = false; gameCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            gameCanvasGroup.alpha = savedCanvasAlpha; gameCanvasGroup.interactable = savedCanvasInteractable; gameCanvasGroup.blocksRaycasts = savedCanvasRaycasts;
        }
        gameCanvasHidden = hidden;
    }
    public static void ReleaseTabletScreen()
    {
        if (instance != null && instance.panelRect != null && instance.overlayRoot != null)
            instance.panelRect.SetParent(instance.overlayRoot, false);
    }
    private Button Button(Transform parent,string label,float x,float y,float w,float h,Action action)
    { var b=UIFactory.CreateButton(label,parent,label,new Vector2(x,y),new Vector2(x+w,y+h),currentSkin.buttonPrimary,currentSkin); b.onClick.AddListener(()=>{localNotice=null; action(); if(config.click!=null)sound.PlayOneShot(config.click,.2f);}); return b; }
    private TMP_Text Text(Transform parent,string text,float x,float y,float w,float h,float size=22)
    { var t=UIFactory.CreateText("Text",parent,text,size,new Vector2(x,y),new Vector2(x+w,y+h),TextAlignmentOptions.TopLeft,currentSkin.bodyColor,currentSkin); t.richText=false;t.raycastTarget=false;return t; }
    private TMP_InputField Input(Transform parent,float x,float y,float w,float h,string placeholder,int limit)
    {
        var go=UIFactory.CreatePanel("Input",parent,new Vector2(x,y),new Vector2(x+w,y+h),new Color(.07f,.12f,.16f),0,currentSkin);
        var field=go.AddComponent<TMP_InputField>(); var text=Text(go.transform,"",.025f,.07f,.95f,.86f,20); var ph=Text(go.transform,placeholder,.025f,.07f,.95f,.86f,20);
        ph.color=new Color(.55f,.65f,.7f); field.textViewport=go.GetComponent<RectTransform>();field.textComponent=(TextMeshProUGUI)text;field.placeholder=ph;field.characterLimit=limit;
        field.targetGraphic=go.GetComponent<Image>(); field.readOnly=false;
        field.caretColor=Color.white; field.customCaretColor=true; field.caretWidth=2;
        field.onSelect.AddListener(_=>CyberDefense.UI.HeatSessionUI.RefreshGameplayInput());
        field.onDeselect.AddListener(_=>CyberDefense.UI.HeatSessionUI.RefreshGameplayInput());return field;
    }
    private void Build()
    {
        var canvas=UIFactory.CreateCanvas("Mission HUD and tablet",transform,250,new Vector2(1920,1080));
        overlayRoot=canvas.transform;
        hudPanel=UIFactory.CreatePanel("HUD",canvas.transform,new Vector2(.025f,.89f),new Vector2(.46f,.975f),new Color(.015f,.03f,.05f,.72f),0,currentSkin);
        hudPanel.GetComponent<Image>().raycastTarget=false;
        hud=Text(hudPanel.transform,"",.025f,.08f,.95f,.86f,20);
        controls=Text(canvas.transform,"TAB Tablet   ·   F2 Chat   ·   F3 Equipo   ·   ESC Menu",.37f,.018f,.61f,.065f,18);
        controls.alignment=TextAlignmentOptions.BottomRight;
        toast=Text(canvas.transform,"",.16f,.72f,.68f,.08f,24);toast.color=currentSkin.titleColor;
        panel=UIFactory.CreatePanel("Tablet screen",canvas.transform,new Vector2(.09f,.08f),new Vector2(.91f,.92f),currentSkin.panelBackground,0,currentSkin);
        panelRect=panel.GetComponent<RectTransform>(); group=panel.AddComponent<CanvasGroup>();
        title=Text(panel.transform,"TABLET",.04f,.94f,.84f,.055f,26);title.color=currentSkin.titleColor;
        Button(panel.transform,"X",.93f,.945f,.04f,.045f,()=>Open(false));
        string[] tabLabels={"OPERACIONES","TICKETS","HISTORIAL","TUTORIAL"};
        for(int i=0;i<tabs.Length;i++){int target=i;tabs[i]=Button(panel.transform,tabLabels[i],.04f+i*.23f,.87f,.21f,.055f,()=>{page=target;localNotice=null;});}
        var scrollGo=new GameObject("Scrollable content",typeof(RectTransform),typeof(ScrollRect)); scrollGo.transform.SetParent(panel.transform,false);
        var sr=scrollGo.GetComponent<RectTransform>(); sr.anchorMin=new Vector2(.04f,.28f);sr.anchorMax=new Vector2(.62f,.83f);sr.offsetMin=sr.offsetMax=Vector2.zero;
        scrollGo.AddComponent<RectMask2D>(); var bg=scrollGo.AddComponent<Image>();bg.color=new Color(0,0,0,.1f);
        var content=new GameObject("Content",typeof(RectTransform),typeof(ContentSizeFitter));content.transform.SetParent(scrollGo.transform,false);
        var cr=content.GetComponent<RectTransform>();cr.anchorMin=new Vector2(0,1);cr.anchorMax=Vector2.one;cr.pivot=new Vector2(.5f,1);cr.offsetMin=cr.offsetMax=Vector2.zero;
        body=content.AddComponent<TextMeshProUGUI>();body.fontSize=22;body.color=currentSkin.bodyColor;body.richText=false;
        if(currentSkin.font!=null)body.font=currentSkin.font;
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=scrollGo.GetComponent<ScrollRect>();scroll.viewport=sr;scroll.content=cr;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
        bodyViewport=scrollGo;
        for(int i=0;i<4;i++) options[i]=Button(panel.transform,"",.65f,.71f-i*.11f,.31f,.095f,()=>{});
        primary=Button(panel.transform,"",.65f,.17f,.31f,.075f,()=>primaryAction?.Invoke());
        secondary=Button(panel.transform,"",.65f,.07f,.31f,.075f,()=>secondaryAction?.Invoke());
        hint=Button(panel.transform,"Pista",.65f,.27f,.31f,.055f,()=>Mission.RequestHint());
        identifier=Input(panel.transform,.04f,.17f,.57f,.07f,"Identificador",32);
        error=Text(panel.transform,"",.04f,.055f,.57f,.095f,18);error.color=currentSkin.errorColor;
        var bar=UIFactory.CreatePanel("Progress",panel.transform,new Vector2(.04f,.015f),new Vector2(.96f,.045f),new Color(.1f,.15f,.2f),0,currentSkin);
        var fill=UIFactory.CreatePanel("Fill",bar.transform,Vector2.zero,Vector2.one,currentSkin.titleColor,0,currentSkin);progress=fill.GetComponent<Image>();
        progressLabel=Text(bar.transform,"",.01f,0,.98f,1,18);
        form=UIFactory.CreatePanel("Ticket draft",panel.transform,new Vector2(.04f,.28f),new Vector2(.96f,.83f),currentSkin.panelBackground,0,currentSkin);
        Text(form.transform,"DIAGNOSTICO • pulsa para elegir",0,.88f,.48f,.1f,22);
        Text(form.transform,"EVIDENCIAS • seleccion multiple",.51f,.88f,.49f,.1f,22);
        for(int i=0;i<4;i++){int idx=i;fields[i]=Button(form.transform,"",0,.67f-i*.18f,.48f,.16f,()=>{if(idx==0)diagnosis=(diagnosis+1)%3;else if(idx==1)severity=(severity+1)%3;else if(idx==2)recommendation=(recommendation+1)%3;else system=(system+1)%2;});
            evidence[i]=Button(form.transform,"",.51f,.67f-i*.15f,.49f,.13f,()=>evidenceMask^=1<<idx);}
        comment=Input(form.transform,.51f,.02f,.49f,.16f,"Comentario opcional",128);
        panel.SetActive(false);
    }
}
