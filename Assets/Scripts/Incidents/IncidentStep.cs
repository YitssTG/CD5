using System;
using UnityEngine;

/// <summary>
/// Un objetivo concreto dentro de un incidente. Los pasos son datos: el motor
/// (IncidentEngine) los evalua y CooperativeIncident los sincroniza.
/// Un paso sin minijuego es una confirmacion simple; con minijuego exige una
/// seleccion valida.
/// </summary>
[Serializable]
public class IncidentStep
{
    [Tooltip("Identificador estable del paso.")]
    public string stepId = "step";

    public ObjectiveType objectiveType = ObjectiveType.Investigate;

    [Tooltip("Etapa que se muestra mientras este paso esta activo.")]
    public IncidentStage stage = IncidentStage.Alert;

    [Tooltip("Rol autorizado a completar este paso. El otro rol solo observa.")]
    public RoleType authorizedRole = RoleType.Pentester;

    [Tooltip("Texto corto de objetivo (HUD).")]
    public string objective = "";

    [TextArea(2, 6)] public string briefing = "";

    public string[] hints = Array.Empty<string>();

    [Min(0)] public int successPoints = 0;
    [Min(0)] public int penaltyPoints = 0;
    [Min(0)] public float penaltyDelay = 0f;

    [TextArea(1, 3)] public string successFeedback = "";
    [TextArea(1, 3)] public string failureFeedback = "";

    public MinigameDefinition minigame = new MinigameDefinition();

    [Tooltip("Si es el ultimo paso: al completarse el incidente queda Resolved.")]
    public bool completesIncident = false;

    public bool HasMinigame => minigame != null && minigame.kind != MinigameKind.None;
}
