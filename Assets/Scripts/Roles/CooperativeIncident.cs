using System;
using Fusion;
using UnityEngine;

// Estado autoritativo de la mision cooperativa. Toda decision se valida en el
// StateAuthority; los clientes solo muestran. La logica vive en IncidentEngine.
public class CooperativeIncident : NetworkBehaviour
{
    public static CooperativeIncident Instance { get; private set; }
    [SerializeField] private IncidentDefinition definition;
    public IncidentDefinition Definition => definition;
    private bool spawned;
    public bool IsOnline => spawned && Object != null && Object.IsValid && Runner != null;

    [Networked] public IncidentStage Stage { get; set; }
    [Networked] public IncidentFeedback Feedback { get; set; }
    [Networked] public int IncidentNumber { get; set; }
    [Networked] public int Revision { get; set; }
    [Networked] public int CurrentStep { get; set; }
    [Networked] public int LastFeedbackStep { get; set; }
    [Networked] public int Score { get; set; }
    [Networked] public int Penalty { get; set; }
    [Networked] public int WrongAttempts { get; set; }
    [Networked] public int WrongOnStep { get; set; }
    [Networked] public int HintMask { get; set; }
    [Networked] public PlayerRef Investigator { get; set; }
    [Networked] public PlayerRef Administrator { get; set; }
    [Networked] public TickTimer RetryAfter { get; set; }
    public string LocalError { get; private set; }

    private IncidentStep[] fallbackSteps;

    public bool TeamReady
    {
        get
        {
            var roles = RoleManager.Instance;
            return IsOnline && roles != null && roles.IsOnline && roles.GameStarted &&
                roles.AdminPlayer == Administrator && roles.PentesterPlayer == Investigator &&
                Administrator != Investigator && Active(Administrator) && Active(Investigator);
        }
    }

    public bool IsResolved => Stage == IncidentStage.Resolved;
    public int StepCount => IncidentEngine.StepCount(Steps);

    /// <summary>Pasos efectivos: los configurados o el flujo SQL heredado.</summary>
    public IncidentStep[] Steps
    {
        get
        {
            if (definition == null) return Array.Empty<IncidentStep>();
            if (definition.HasSteps) return definition.steps;
            if (fallbackSteps == null) fallbackSteps = SqlInjectionFlow.Build(definition);
            return fallbackSteps;
        }
    }

    private ScoringConfig Scoring =>
        definition != null && definition.scoring != null ? definition.scoring : DefaultScoring;
    private static readonly ScoringConfig DefaultScoring = new ScoringConfig();
    public int HintCost => Mathf.Max(0, Scoring.hintCost);

    public bool TryGetStep(out IncidentStep step) => IncidentEngine.TryGetStep(Steps, CurrentStep, out step);
    public bool TryGetStepAt(int index, out IncidentStep step) => IncidentEngine.TryGetStep(Steps, index, out step);
    public bool IsHintRevealed(int index) => (HintMask & (1 << index)) != 0;

    public override void Spawned() { spawned = true; Instance = this; }
    public override void Despawned(NetworkRunner runner, bool hasState) { spawned = false; if (Instance == this) Instance = null; }
    private void OnDestroy() { spawned = false; if (Instance == this) Instance = null; }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || definition == null) return;
        var roles = RoleManager.Instance;
        if (roles == null || !roles.IsOnline || !roles.GameStarted ||
            !Active(roles.AdminPlayer) || !Active(roles.PentesterPlayer) || roles.AdminPlayer == roles.PentesterPlayer) return;
        if (Stage == IncidentStage.Waiting || Administrator != roles.AdminPlayer || Investigator != roles.PentesterPlayer)
        {
            // A new team starts a fresh exercise; a master change with the same team keeps networked progress.
            Administrator = roles.AdminPlayer;
            Investigator = roles.PentesterPlayer;
            IncidentNumber++;
            Revision++;
            ResetProgress();
        }
    }

    private void ResetProgress()
    {
        CurrentStep = 0;
        LastFeedbackStep = -1;
        Stage = IncidentEngine.StageFor(Steps, 0);
        Feedback = IncidentFeedback.None;
        Score = Penalty = WrongAttempts = WrongOnStep = 0;
        HintMask = 0;
        RetryAfter = TickTimer.None;
    }

    private bool Active(PlayerRef player)
    {
        if (player == PlayerRef.None || Runner == null) return false;
        foreach (var active in Runner.ActivePlayers) if (active == player) return true;
        return false;
    }

    public Transform FindTerminal(RoleType role)
    {
        // Existing scene objects are the two stations. No duplicated scene manager or spawned terminal prefab.
        var servers = FindObjectsByType<ServidorRed>(FindObjectsSortMode.None);
        if (servers.Length != 1) return null;
        if (role == RoleType.Pentester) return servers[0].transform;
        if (role != RoleType.Admin) return null;
        foreach (var console in FindObjectsByType<ConsolaControl>(FindObjectsSortMode.None))
            if (console.servidorAsociado == servers[0]) return console.transform;
        return null;
    }

    public bool IsNear(PlayerRef player, RoleType role, out float distance)
    {
        distance = float.PositiveInfinity;
        if (!IsOnline || definition == null) return false;
        var avatar = Runner.GetPlayerObject(player);
        var terminal = FindTerminal(role);
        if (avatar == null || !avatar.IsValid || avatar.StateAuthority != player || terminal == null) return false;
        distance = Vector3.Distance(avatar.transform.position, terminal.position);
        return distance <= definition.interactionRadius;
    }

    /// <summary>
    /// Solicita completar el paso actual.
    /// choice = indice (SimpleChoice) o secuencia empaquetada (ServerRepair);
    /// mask   = conjunto de evidencias (RequestAnalysis). -1 = paso sin minijuego.
    /// </summary>
    public void Submit(int choice, int mask = 0)
    {
        if (!IsOnline) return;
        LocalError = null;
        RPC_Submit(IncidentNumber, Revision, CurrentStep, choice, mask);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Submit(int incident, int revision, int step, int choice, int mask, RpcInfo info = default)
    {
        if (!HasStateAuthority || !TeamReady || definition == null || !Active(info.Source)) return;
        // Optimistic revision prevents double clicks, replay and stale actions crossing incident resets.
        if (incident != IncidentNumber || revision != Revision || step != CurrentStep) return;
        var role = RoleManager.Instance.GetRole(info.Source);
        if (!IsNear(info.Source, role, out _)) { RPC_Error(info.Source, "Acercate a tu terminal para actuar."); return; }
        if (!RetryAfter.ExpiredOrNotRunning(Runner)) { RPC_Error(info.Source, "Revisa el resultado mientras termina la penalizacion."); return; }
        if (!IncidentEngine.TryGetStep(Steps, CurrentStep, out var currentStep)) return;
        if (!IncidentEngine.TryEvaluate(Steps, CurrentStep, role, choice, mask, out bool correct, out int delta, out var feedback))
        { RPC_Error(info.Source, "Accion no permitida para tu rol o el paso actual."); return; }

        LastFeedbackStep = CurrentStep;
        if (correct)
        {
            Score += Mathf.Max(0, delta);
            if (WrongOnStep == 0) Score += Mathf.Max(0, Scoring.accuracyBonus);
            Feedback = feedback;
            int next = IncidentEngine.NextStep(Steps, CurrentStep, currentStep);
            CurrentStep = next;
            Stage = IncidentEngine.StageFor(Steps, next);
            WrongOnStep = 0;
            HintMask = 0;
            RetryAfter = TickTimer.None;
            if (Stage == IncidentStage.Resolved) Score += Mathf.Max(0, Scoring.cooperationBonus);
        }
        else
        {
            WrongAttempts++;
            WrongOnStep++;
            Penalty += Mathf.Max(0, -delta);
            if (Scoring.maxPenalty > 0) Penalty = Mathf.Min(Penalty, Scoring.maxPenalty);
            Feedback = feedback;
            if (currentStep.penaltyDelay > 0)
                RetryAfter = TickTimer.CreateFromSeconds(Runner, currentStep.penaltyDelay);
        }
        Revision++;
        Debug.Log($"[Incident] {definition.incidentId}/{IncidentNumber} revision={Revision} actor={info.Source} " +
            $"step={CurrentStep} correct={correct} stage={Stage} score={Score} penalty={Penalty}");
    }

    /// <summary>Revela la siguiente pista del paso actual (coste en puntos).</summary>
    public void RequestHint()
    {
        if (!IsOnline) return;
        LocalError = null;
        RPC_Hint(IncidentNumber, Revision, CurrentStep);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Hint(int incident, int revision, int step, RpcInfo info = default)
    {
        if (!HasStateAuthority || !TeamReady || definition == null || !Active(info.Source)) return;
        if (incident != IncidentNumber || revision != Revision || step != CurrentStep) return;
        var role = RoleManager.Instance.GetRole(info.Source);
        if (!IsNear(info.Source, role, out _)) { RPC_Error(info.Source, "Acercate a tu terminal para actuar."); return; }
        if (!IncidentEngine.TryGetStep(Steps, CurrentStep, out var currentStep)) return;

        int next = 0;
        if (currentStep.hints != null)
            while (next < currentStep.hints.Length && IsHintRevealed(next)) next++;
        if (currentStep.hints == null || next >= currentStep.hints.Length)
        { RPC_Error(info.Source, "No hay mas pistas para este paso."); return; }

        HintMask |= 1 << next;
        Penalty += Mathf.Max(0, Scoring.hintCost);
        if (Scoring.maxPenalty > 0) Penalty = Mathf.Min(Penalty, Scoring.maxPenalty);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Error(PlayerRef target, NetworkString<_128> message)
    { if (Runner.LocalPlayer == target) LocalError = message.ToString(); }
}
