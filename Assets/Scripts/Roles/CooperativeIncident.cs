using System;
using System.Text;
using Fusion;
using UnityEngine;

// Single state authority for tutorial, mission, tickets, operations and journal.
public class CooperativeIncident : NetworkBehaviour
{
    public static CooperativeIncident Instance { get; private set; }
    [SerializeField] private IncidentDefinition definition;
    public IncidentDefinition Definition => definition;
    private bool spawned;
    private int presentedRecord;
    public event Action<MissionRecord> EventPresented;
    public bool IsOnline => spawned && Object != null && Object.IsValid && Runner != null;
    [Networked] public MissionPhase Phase { get; set; }
    [Networked] public IncidentStage Stage { get; set; }
    [Networked] public IncidentFeedback Feedback { get; set; }
    [Networked] public int IncidentNumber { get; set; }
    [Networked] public int Revision { get; set; }
    [Networked] public int CurrentStep { get; set; }
    [Networked] public NetworkBool IsPhishing { get; set; }
    [Networked] public int SqlCampaignScore { get; set; }
    [Networked] public int LastFeedbackStep { get; set; }
    [Networked] public int Score { get; set; }
    [Networked] public int Penalty { get; set; }
    [Networked] public int WrongAttempts { get; set; }
    [Networked] public int WrongOnStep { get; set; }
    [Networked] public int HintMask { get; set; }
    [Networked] public int HintsUsed { get; set; }
    [Networked] public int PentesterPoints { get; set; }
    [Networked] public int AdminPoints { get; set; }
    [Networked] public PlayerRef Investigator { get; set; }
    [Networked] public PlayerRef Administrator { get; set; }
    [Networked] public NetworkBool PentesterReady { get; set; }
    [Networked] public NetworkBool AdminReady { get; set; }
    [Networked] public NetworkBool ServerIdentified { get; set; }
    [Networked] public float TutorialStartedAt { get; set; }
    [Networked] public float StartedAt { get; set; }
    [Networked] public float EndedAt { get; set; }
    [Networked] public float TutorialSeconds { get; set; }
    [Networked] public TickTimer RetryAfter { get; set; }
    [Networked] public TickTimer OperationTimer { get; set; }
    [Networked] public NetworkBool Operating { get; set; }
    [Networked] public float OperationDuration { get; set; }
    [Networked] public PlayerRef Operator { get; set; }
    [Networked] public int PendingChoice { get; set; }
    [Networked] public int PendingMask { get; set; }
    [Networked] public IncidentTicket Ticket { get; set; }
    [Networked] public TicketStatus TicketState { get; set; }
    [Networked] public int TicketUnlockedVersion { get; set; }
    [Networked] public int TicketCount { get; set; }
    [Networked] public int ReturnedTickets { get; set; }
    [Networked] public int RecordCount { get; set; }
    [Networked, Capacity(MissionNetworkBudget.EventCapacity)] public NetworkArray<MissionRecord> Records => default;
    [Networked, Capacity(MissionNetworkBudget.TicketCapacity)] public NetworkArray<IncidentTicket> TicketHistory => default;
    [Networked] public int AdminReplay { get; set; }
    [Networked] public int PentesterReplay { get; set; }
    public string LocalError { get; private set; }
    private IncidentStep[] fallbackSteps;
    private IncidentStep[] phishingSteps;
    private float Now => (float)Runner.SimulationTime;
    public bool IsResolved => Stage == IncidentStage.Resolved;
    public bool Finished => Phase == MissionPhase.Complete || Phase == MissionPhase.Interrupted;
    public float Elapsed => StartedAt <= 0 ? 0 : Mathf.Max(0, (Finished ? EndedAt : Now) - StartedAt);
    public int NetScore => Score - Penalty;
    public float Progress => !Operating ? 0 : Mathf.Clamp01(1 - (OperationTimer.RemainingTime(Runner) ?? 0) / Mathf.Max(.1f, OperationDuration));
    public bool TeamReady => IsOnline && Active(Administrator) && Active(Investigator) && Administrator != Investigator &&
        RoleManager.Instance != null && RoleManager.Instance.IsOnline && RoleManager.Instance.AdminPlayer == Administrator &&
        RoleManager.Instance.PentesterPlayer == Investigator && RoleManager.Instance.GameStarted;
    public IncidentStep[] Steps => IsPhishing ? phishingSteps ?? (phishingSteps = SqlInjectionFlow.BuildPhishing()) : definition == null ? Array.Empty<IncidentStep>() : definition.HasSteps ? definition.steps :
        fallbackSteps ?? (fallbackSteps = SqlInjectionFlow.Build(definition));
    public int StepCount => IncidentEngine.StepCount(Steps);
    private ScoringConfig Scoring => definition.scoring ?? new ScoringConfig();
    public int HintCost => Mathf.Max(0, Scoring.hintCost);
    public bool TryGetStep(out IncidentStep step) => IncidentEngine.TryGetStep(Steps, CurrentStep, out step);
    public bool TryGetStepAt(int index, out IncidentStep step) => IncidentEngine.TryGetStep(Steps, index, out step);
    public bool IsHintRevealed(int index) => index >= 0 && index < 30 && (HintMask & (1 << index)) != 0;
    public override void Spawned() { spawned = true; Instance = this; }
    public override void Despawned(NetworkRunner runner, bool hasState) { spawned = false; if (Instance == this) Instance = null; }
    private void OnDestroy() { spawned = false; if (Instance == this) Instance = null; }
    public override void Render()
    {
        if (presentedRecord > RecordCount) presentedRecord = 0;
        int first = Mathf.Max(presentedRecord, RecordCount - Records.Length);
        for (int i = first; i < RecordCount; i++) EventPresented?.Invoke(Records[i % Records.Length]);
        presentedRecord = RecordCount;
    }
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || definition == null) return;
        if ((Phase == MissionPhase.Active || Phase == MissionPhase.Tutorial) && !TeamReady)
        { Interrupt("El equipo se desconecto o cambio durante la sesion."); return; }
        if (Phase == MissionPhase.Waiting)
        {
            var r = RoleManager.Instance;
            if (r == null || !r.IsOnline || !r.GameStarted || !Active(r.AdminPlayer) || !Active(r.PentesterPlayer) || r.AdminPlayer == r.PentesterPlayer) return;
            Administrator = r.AdminPlayer; Investigator = r.PentesterPlayer;
            ResetSession();
        }
        if (Operating)
        {
            RoleType role = RoleManager.Instance.GetRole(Operator);
            if (!IsNear(Operator, role, out _)) { CancelOperation("Operacion cancelada: el operador se alejo."); return; }
            if (OperationTimer.Expired(Runner))
            {
                Operating = false;
                if (IncidentEngine.TryEvaluate(Steps, CurrentStep, role, PendingChoice, PendingMask, out bool correct, out int delta, out var feedback))
                    ApplyAnswer(Operator, correct, delta, feedback);
            }
        }
    }
    private void ResetSession()
    {
        IncidentNumber++; Revision++; CurrentStep = 0; LastFeedbackStep = -1;
        Phase = MissionPhase.Tutorial; Stage = IncidentStage.Waiting;
        Score = Penalty = WrongAttempts = WrongOnStep = HintMask = HintsUsed = PentesterPoints = AdminPoints = 0;
        PentesterReady = AdminReady = ServerIdentified = Operating = false;
        StartedAt = EndedAt = TutorialSeconds = 0; TutorialStartedAt = Now;
        RetryAfter = OperationTimer = TickTimer.None;
        Ticket = default; TicketState = TicketStatus.None; TicketUnlockedVersion = TicketCount = ReturnedTickets = 0;
        AdminReplay = PentesterReplay = 0; Feedback = IncidentFeedback.None;
        // Keep earlier session history until bounded capacity is reached; events carry the incident number.
        Record(MissionEventKind.TeamReady, PlayerRef.None, 0, "Equipo preparado. Exploracion y tutorial disponibles.");
    }
    private bool Active(PlayerRef p)
    { if (p == PlayerRef.None || Runner == null) return false; foreach (var a in Runner.ActivePlayers) if (a == p) return true; return false; }
    public Transform FindTerminal(RoleType role)
    {
        var servers = FindObjectsByType<ServidorRed>(FindObjectsSortMode.None);
        if (servers.Length != 1) return null;
        if (role == RoleType.Pentester) return servers[0].transform;
        if (role == RoleType.Admin) foreach (var console in FindObjectsByType<ConsolaControl>(FindObjectsSortMode.None))
            if (console.servidorAsociado == servers[0]) return console.transform;
        return null;
    }
    public bool IsNear(PlayerRef player, RoleType role, out float distance)
    {
        distance = float.PositiveInfinity;
        if (!IsOnline || definition == null) return false;
        var avatar = Runner.GetPlayerObject(player); var terminal = FindTerminal(role);
        if (avatar == null || !avatar.IsValid || avatar.StateAuthority != player || terminal == null) return false;
        distance = Vector3.Distance(avatar.transform.position, terminal.position);
        return distance <= definition.interactionRadius;
    }
    private bool Valid(PlayerRef p, int incident, int revision, RoleType role, bool near = false)
    {
        if (!HasStateAuthority || Phase != MissionPhase.Active || !TeamReady || Operating || incident != IncidentNumber || revision != Revision ||
            !Active(p) || RoleManager.Instance.GetRole(p) != role) return false;
        if (!RetryAfter.ExpiredOrNotRunning(Runner)) { RPC_Error(p, "Espera a que termine la penalizacion."); return false; }
        if (near && !IsNear(p, role, out _)) { RPC_Error(p, "Acercate a tu estacion para realizar esta operacion."); return false; }
        return true;
    }
    public void ClearError() { LocalError = null; }
    public void ConfirmTutorial(bool skipped) { if (IsOnline) RPC_Tutorial(IncidentNumber, skipped); }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Tutorial(int incident, bool skipped, RpcInfo info = default)
    {
        if (!HasStateAuthority || Phase != MissionPhase.Tutorial || !TeamReady || incident != IncidentNumber) return;
        if (info.Source == Investigator && !PentesterReady) PentesterReady = true;
        else if (info.Source == Administrator && !AdminReady) AdminReady = true;
        else return;
        Record(MissionEventKind.TutorialComplete, info.Source, 0, skipped ? "Tutorial omitido explicitamente; preparado." : "Tutorial completado; preparado.");
        Revision++;
        if (PentesterReady && AdminReady)
        {
            TutorialSeconds = Mathf.Max(0, Now - TutorialStartedAt); StartedAt = Now;
            Phase = MissionPhase.Active; Stage = IncidentEngine.StageFor(Steps, 0);
            Record(MissionEventKind.Started, PlayerRef.None, 0, IsPhishing ? "Alerta: correo sospechoso en el laboratorio." : "Alerta: acceso anomalo al portal de matriculas.");
        }
    }
    public void Identify(string id) { if (IsOnline) { ClearError(); RPC_Identify(IncidentNumber, Revision, id ?? ""); } }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Identify(int incident, int revision, NetworkString<_32> id, RpcInfo info = default)
    {
        if (!Valid(info.Source, incident, revision, RoleType.Pentester, true)) return;
        if (!string.Equals(id.ToString().Trim(), definition.serverId, StringComparison.OrdinalIgnoreCase))
        { RPC_Error(info.Source, "ID desconocido. Consulta la etiqueta del servidor. Sin penalizacion."); return; }
        if (ServerIdentified) return;
        ServerIdentified = true; Revision++;
        Record(MissionEventKind.Evidence, info.Source, 0, "Servidor identificado: " + definition.serverId);
    }
    public void Submit(int choice, int mask = 0) { if (IsOnline) { ClearError(); RPC_Submit(IncidentNumber, Revision, CurrentStep, choice, mask); } }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Submit(int incident, int revision, int step, int choice, int mask, RpcInfo info = default)
    {
        if (!TryGetStep(out var current) || step != CurrentStep || (!IsPhishing && (step == 2 || step == 3))) return;
        if (!Valid(info.Source, incident, revision, current.authorizedRole, true)) return;
        if (!IsPhishing && current.authorizedRole == RoleType.Pentester && !ServerIdentified) { RPC_Error(info.Source, "Introduce primero el ID del servidor en la tablet."); return; }
        if (!IsPhishing && step >= 4 && TicketState != TicketStatus.Accepted) return;
        if (!IncidentEngine.TryEvaluate(Steps, step, current.authorizedRole, choice, mask, out bool correct, out int delta, out var feedback))
        { RPC_Error(info.Source, "Seleccion incompleta o invalida."); return; }
        float seconds = IsPhishing ? 0 : step == 0 ? definition.analysisSeconds : step == 4 ? definition.containmentSeconds : step == 5 ? definition.repairSeconds : step == 6 ? definition.verificationSeconds : 0;
        if (!correct || seconds <= 0) { ApplyAnswer(info.Source, correct, delta, feedback); return; }
        Operator = info.Source; PendingChoice = choice; PendingMask = mask;
        OperationDuration = Mathf.Max(.5f, seconds); OperationTimer = TickTimer.CreateFromSeconds(Runner, OperationDuration);
        Operating = true; Revision++;
        Record(MissionEventKind.OperationStarted, info.Source, 0, current.objective);
    }
    private void ApplyAnswer(PlayerRef actor, bool correct, int delta, IncidentFeedback feedback)
    {
        if (!TryGetStep(out var step)) return;
        LastFeedbackStep = CurrentStep; Feedback = feedback;
        if (correct)
        {
            int reward = Mathf.Max(0, delta) + (WrongOnStep == 0 ? Mathf.Max(0, Scoring.accuracyBonus) : 0);
            Award(actor, reward);
            var kind = CurrentStep == 4 ? MissionEventKind.Contained : CurrentStep == 5 ? MissionEventKind.Repaired : CurrentStep == 6 ? MissionEventKind.Verified : MissionEventKind.Evidence;
            Record(kind, actor, reward, step.successFeedback);
            CurrentStep = IncidentEngine.NextStep(Steps, CurrentStep, step);
            Stage = IncidentEngine.StageFor(Steps, CurrentStep); WrongOnStep = HintMask = 0; RetryAfter = TickTimer.None;
            if (Stage == IncidentStage.Resolved)
            {
                Phase = MissionPhase.Complete; EndedAt = Now;
                int bonus = Mathf.Max(0, Scoring.cooperationBonus); Score += bonus;
                Record(MissionEventKind.Resolved, PlayerRef.None, bonus, IsPhishing ? "Respuesta al phishing verificada en equipo." : "Servicio restaurado tras verificacion cooperativa.");
            }
        }
        else
        {
            WrongAttempts++; WrongOnStep++;
            int paid = Charge(actor, Mathf.Max(0, -delta));
            Record(MissionEventKind.Error, actor, -paid, step.failureFeedback);
            if (step.penaltyDelay > 0) RetryAfter = TickTimer.CreateFromSeconds(Runner, step.penaltyDelay);
        }
        Revision++;
    }
    private void Award(PlayerRef actor, int points) { Score += points; if (actor == Investigator) PentesterPoints += points; else if (actor == Administrator) AdminPoints += points; }
    private int Charge(PlayerRef actor, int cost)
    {
        int paid = Mathf.Max(0, cost); if (Scoring.maxPenalty > 0) paid = Mathf.Min(paid, Mathf.Max(0, Scoring.maxPenalty - Penalty));
        Penalty += paid; if (actor == Investigator) PentesterPoints -= paid; else if (actor == Administrator) AdminPoints -= paid;
        return paid;
    }
    private void CancelOperation(string reason)
    { if (!Operating) return; Operating = false; OperationTimer = TickTimer.None; Revision++; Record(MissionEventKind.OperationCancelled, Operator, 0, reason); }
    public void SendTicket(int diagnosis, int mask, int severity, int recommendation, int system, string comment)
    { if (IsOnline) { ClearError(); RPC_Ticket(IncidentNumber, Revision, diagnosis, mask, severity, recommendation, system, TicketRules.NormalizeComment(comment)); } }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Ticket(int incident, int revision, int diagnosis, int mask, int severity, int recommendation, int system, string comment, RpcInfo info = default)
    {
        if (!Valid(info.Source, incident, revision, RoleType.Pentester) || CurrentStep != 2 || !TicketRules.IsValid(diagnosis, mask, severity, recommendation, system)) return;
        TicketCount++;
        Ticket = new IncidentTicket { Version = TicketCount, Seconds = Elapsed, Author = info.Source, Diagnosis = diagnosis, EvidenceMask = mask,
            Severity = severity, Recommendation = recommendation, System = system, Comment = TicketRules.NormalizeComment(comment) };
        TicketHistory.Set((TicketCount - 1) % TicketHistory.Length, Ticket);
        TicketState = TicketStatus.Submitted; TicketUnlockedVersion = 0; CurrentStep = 3; Stage = IncidentStage.Reported; Revision++;
        Record(MissionEventKind.TicketSent, info.Source, 0, TicketRules.Code(IncidentNumber, TicketCount) + " enviado para revision.");
        Record(MissionEventKind.TicketReceived, Administrator, 0, "Ticket recibido. Introduce el codigo en tu PC.");
    }
    public void UnlockTicket(string code) { if (IsOnline) { ClearError(); RPC_UnlockTicket(IncidentNumber, Revision, code ?? ""); } }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_UnlockTicket(int incident, int revision, NetworkString<_32> code, RpcInfo info = default)
    {
        if (!Valid(info.Source, incident, revision, RoleType.Admin, true) || CurrentStep != 3) return;
        if (!string.Equals(code.ToString().Trim(), TicketRules.Code(IncidentNumber, Ticket.Version), StringComparison.OrdinalIgnoreCase))
        { RPC_Error(info.Source, "Codigo no encontrado. Copia el codigo mostrado en la tablet."); return; }
        if (TicketUnlockedVersion == Ticket.Version) return;
        TicketUnlockedVersion = Ticket.Version; Revision++;
    }
    public void ReviewTicket(bool accept) { if (IsOnline) { ClearError(); RPC_ReviewTicket(IncidentNumber, Revision, accept); } }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ReviewTicket(int incident, int revision, bool accept, RpcInfo info = default)
    {
        if (!Valid(info.Source, incident, revision, RoleType.Admin, true) || CurrentStep != 3 || TicketState != TicketStatus.Submitted || TicketUnlockedVersion != Ticket.Version) return;
        bool correct = TicketRules.IsCorrect(Ticket.Diagnosis, Ticket.EvidenceMask, Ticket.Severity, Ticket.Recommendation, Ticket.System);
        if (!correct)
        {
            int paid = Charge(Investigator, definition.wrongReportPenalty); WrongAttempts++;
            Record(MissionEventKind.Error, Investigator, -paid, "Ticket inconsistente: contrasta consulta alterada, acceso no autorizado y servicio afectado.");
            if (accept) { int adminPaid = Charge(Administrator, definition.wrongReportPenalty); Record(MissionEventKind.Error, Administrator, -adminPaid, "La revision aceptaba un reporte inconsistente. Devuelto para corregir."); }
        }
        if (!accept || !correct)
        {
            TicketState = TicketStatus.Returned; ReturnedTickets++; CurrentStep = 2; Stage = IncidentStage.Diagnosed;
            Record(MissionEventKind.TicketReturned, info.Source, 0, "Solicitada correccion de " + TicketRules.Code(IncidentNumber, Ticket.Version));
        }
        else
        {
            TicketState = TicketStatus.Accepted; CurrentStep = 4; Stage = IncidentStage.Reported;
            Award(Investigator, 15); Award(Administrator, 5);
            Record(MissionEventKind.TicketAccepted, Investigator, 15, "Reporte correcto aceptado.");
            Record(MissionEventKind.TicketAccepted, Administrator, 5, "Revision completada. Contencion autorizada.");
        }
        HintMask = 0; Revision++;
    }
    public void RequestHint() { if (IsOnline) { ClearError(); RPC_Hint(IncidentNumber, Revision, CurrentStep); } }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Hint(int incident, int revision, int step, RpcInfo info = default)
    {
        if (step != CurrentStep || !TryGetStep(out var current) || !Valid(info.Source, incident, revision, current.authorizedRole)) return;
        int next = 0; while (next < 30 && IsHintRevealed(next)) next++;
        if (current.hints == null || next >= current.hints.Length || next >= 30) { RPC_Error(info.Source, "No hay mas pistas."); return; }
        HintMask |= 1 << next; HintsUsed++; int paid = Charge(info.Source, HintCost); Revision++;
        Record(MissionEventKind.Hint, info.Source, -paid, current.hints[next]);
    }
    public void PlayerDeparted(PlayerRef player)
    { if (IsOnline && HasStateAuthority && !Finished && Phase != MissionPhase.Waiting && (player == Administrator || player == Investigator)) Interrupt("Un participante abandono la sesion."); }
    public void Interrupt(string reason)
    {
        if (!HasStateAuthority || Finished || Phase == MissionPhase.Waiting) return;
        CancelOperation("Operacion cancelada por desconexion.");
        if (Phase == MissionPhase.Tutorial) TutorialSeconds = Mathf.Max(0, Now - TutorialStartedAt);
        Phase = MissionPhase.Interrupted; EndedAt = Now; Revision++;
        Record(MissionEventKind.Disconnected, PlayerRef.None, 0, reason);
        Record(MissionEventKind.Interrupted, PlayerRef.None, 0, "Sesion interrumpida. Vuelve al menu para crear otra sala.");
    }
    public void RequestReplay(bool swap) { if (IsOnline) RPC_Replay(IncidentNumber, swap); }
    public void RequestPhishing() { if (IsOnline) RPC_NextIncident(IncidentNumber); }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_NextIncident(int incident, RpcInfo info = default)
    {
        if (!HasStateAuthority || Phase != MissionPhase.Complete || IsPhishing || !TeamReady || incident != IncidentNumber) return;
        if (info.Source == Administrator) AdminReplay = 3; else if (info.Source == Investigator) PentesterReplay = 3; else return;
        if (AdminReplay != 3 || PentesterReplay != 3) return;
        SqlCampaignScore = NetScore;
        IsPhishing = true;
        ResetSession();
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Replay(int incident, bool swap, RpcInfo info = default)
    {
        if (!HasStateAuthority || Phase != MissionPhase.Complete || !TeamReady || incident != IncidentNumber) return;
        int vote = swap ? 2 : 1;
        if (info.Source == Administrator) AdminReplay = vote; else if (info.Source == Investigator) PentesterReplay = vote; else return;
        if (AdminReplay == 0 || AdminReplay != PentesterReplay) return;
        if (swap) { RoleManager.Instance.SwapForReplay(); var p = Administrator; Administrator = Investigator; Investigator = p; }
        ResetSession();
    }
    private void Record(MissionEventKind kind, PlayerRef actor, int points, string detail)
    {
        string message = $"#{IncidentNumber} {detail}";
        if (message.Length > MissionNetworkBudget.EventTextCapacity) message = message.Substring(0, MissionNetworkBudget.EventTextCapacity);
        var record = new MissionRecord { Sequence = RecordCount + 1, Seconds = Elapsed, Actor = actor,
            Role = actor == Investigator ? RoleType.Pentester : actor == Administrator ? RoleType.Admin : RoleType.SinAsignar,
            Kind = kind, Points = points, Detail = message };
        Records.Set(RecordCount % Records.Length, record); RecordCount++;
    }
    public string Journal()
    {
        var b = new StringBuilder();
        if (RecordCount > Records.Length) b.AppendLine($"Historial visible: ultimos {Records.Length} eventos. Totales conservados.");
        for (int i = Mathf.Max(0, RecordCount - Records.Length); i < RecordCount; i++)
        { var r = Records[i % Records.Length]; b.AppendLine($"{TimeSpan.FromSeconds(r.Seconds):mm\\:ss} | {r.Role} | {r.Detail} ({r.Points:+0;-0;0})"); }
        return b.ToString();
    }
    public string Summary()
    {
        var tickets = new StringBuilder();
        if (TicketCount > TicketHistory.Length) tickets.AppendLine($"Versiones visibles: ultimas {TicketHistory.Length}; contador total conservado.");
        for (int i = Mathf.Max(0, TicketCount - TicketHistory.Length); i < TicketCount; i++)
        {
            var t = TicketHistory[i % TicketHistory.Length];
            tickets.AppendLine($"{TicketRules.Code(IncidentNumber, t.Version)} | {TicketRules.DiagnosisName(t.Diagnosis)} | gravedad {TicketRules.SeverityName(t.Severity)} | evidencias: {TicketRules.EvidenceNames(t.EvidenceMask)} | {TicketRules.RecommendationName(t.Recommendation)} | {t.Comment}");
        }
        return $"{(Phase == MissionPhase.Complete ? "INCIDENTE RESUELTO" : "SESION INTERRUMPIDA")} - {(IsPhishing ? "PHISHING" : "SQL INJECTION")}\n" +
            (IsPhishing ? $"Progreso anterior SQL Injection: {SqlCampaignScore} puntos\n" : "") +
            $"Mision: {TimeSpan.FromSeconds(Elapsed):mm\\:ss} | Tutorial: {TimeSpan.FromSeconds(Phase == MissionPhase.Tutorial ? Mathf.Max(0, Now - TutorialStartedAt) : TutorialSeconds):mm\\:ss}\n" +
            $"Resultado: {NetScore} puntos = {Score} ganados - {Penalty} penalizacion\n" +
            $"Pentester: {PentesterPoints} | Administrador: {AdminPoints} (bonus de equipo separado)\n" +
            $"Tickets: {TicketCount} | Devueltos: {ReturnedTickets} | Errores: {WrongAttempts} | Pistas: {HintsUsed}\n\n" +
            (Phase == MissionPhase.Complete ? (IsPhishing ? "El phishing utiliza mensajes enganosos para inducir acciones o robar credenciales. Verifica el remitente y el dominio mediante un canal conocido. Contener incluye revocar sesiones y restablecer el acceso; verifica tambien que el usuario legitimo pueda entrar. Todo el ejercicio es simulado." : definition.explanation) : "El equipo se interrumpio. No se acredita una resolucion.") + "\n\nVERSIONES DE TICKETS\n" + tickets;
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Error(PlayerRef target, string message) { if (Runner.LocalPlayer == target) LocalError = message; }
}
