using System;

/// <summary>
/// Flujo SQL Injection heredado, expresado con el modelo de pasos de la Tarea 03.5.
/// Se usa como respaldo cuando un IncidentDefinition no tiene pasos configurados,
/// de modo que la Tarea 03 sigue funcionando identica sin tocar el asset.
/// </summary>
public static class SqlInjectionFlow
{
    public static IncidentStep[] Build(IncidentDefinition definition)
    {
        string evidence = definition != null ? definition.evidence : "";
        string explanation = definition != null ? definition.explanation : "";
        float delay = definition != null ? Math.Max(0f, definition.wrongReportDelay) : 5f;
        int penalty = definition != null ? Math.Max(0, definition.wrongReportPenalty) : 10;

        return new[]
        {
            new IncidentStep
            {
                stepId = "investigate",
                objectiveType = ObjectiveType.Investigate,
                stage = IncidentStage.Alert,
                authorizedRole = RoleType.Pentester,
                objective = "Leer y registrar las evidencias del acceso anomalo.",
                briefing = evidence,
                successPoints = 5,
                successFeedback = "Evidencias registradas. El Administrador espera tu diagnostico."
            },
            new IncidentStep
            {
                stepId = "diagnose",
                objectiveType = ObjectiveType.Diagnose,
                stage = IncidentStage.Investigating,
                authorizedRole = RoleType.Pentester,
                objective = "Identificar la causa raiz del acceso.",
                briefing = evidence,
                successPoints = 15,
                penaltyPoints = penalty,
                penaltyDelay = delay,
                successFeedback = "Diagnostico correcto: la entrada se concatena en la consulta SQL.",
                failureFeedback = "Esa causa no explica la alteracion de la consulta. Revisa la evidencia.",
                minigame = new MinigameDefinition
                {
                    kind = MinigameKind.SimpleChoice,
                    prompt = "Selecciona la causa raiz.",
                    options = new[]
                    {
                        new MinigameOption { id = "sqli", label = "Inyeccion SQL", isCorrect = true,
                            detail = "Datos del usuario concatenados con instrucciones." },
                        new MinigameOption { id = "brute", label = "Fuerza bruta", isDistractor = true,
                            detail = "Habria muchos intentos fallidos; no se observan." },
                        new MinigameOption { id = "phish", label = "Phishing", isDistractor = true,
                            detail = "No hay engaño por correo en la evidencia." }
                    },
                    wrongFeedback = "Retoma la evidencia: el campo usuario altero la estructura de la consulta."
                }
            },
            new IncidentStep
            {
                stepId = "report",
                objectiveType = ObjectiveType.Report,
                stage = IncidentStage.Diagnosed,
                authorizedRole = RoleType.Pentester,
                objective = "Enviar el reporte tecnico al Administrador.",
                briefing = evidence,
                successPoints = 5,
                successFeedback = "Reporte recibido: el Administrador debe contener el servicio."
            },
            new IncidentStep
            {
                stepId = "contain",
                objectiveType = ObjectiveType.Repair,
                stage = IncidentStage.Reported,
                authorizedRole = RoleType.Admin,
                objective = "Contener el servicio afectado.",
                briefing = evidence,
                successPoints = 15,
                penaltyPoints = penalty,
                penaltyDelay = delay,
                successFeedback = "Portal de matriculas aislado temporalmente. La causa aun existe.",
                failureFeedback = "Ese sistema no es el afectado. Los registros apuntan al portal.",
                minigame = new MinigameDefinition
                {
                    kind = MinigameKind.SimpleChoice,
                    prompt = "Selecciona el servicio a contener.",
                    options = new[]
                    {
                        new MinigameOption { id = "portal", label = "Portal de matriculas", isCorrect = true },
                        new MinigameOption { id = "mail", label = "Servicio de correo", isDistractor = true }
                    },
                    wrongFeedback = "La evidencia senala el portal de matriculas, no el correo."
                }
            },
            new IncidentStep
            {
                stepId = "repair",
                objectiveType = ObjectiveType.Repair,
                stage = IncidentStage.Contained,
                authorizedRole = RoleType.Admin,
                objective = "Aplicar la medida que elimina la causa.",
                briefing = explanation,
                successPoints = 20,
                penaltyPoints = penalty,
                penaltyDelay = delay,
                successFeedback = "Consultas parametrizadas aplicadas. El Pentester debe verificar.",
                failureFeedback = "Esa medida no separa datos de instrucciones. El servicio sigue contenido.",
                minigame = new MinigameDefinition
                {
                    kind = MinigameKind.SimpleChoice,
                    prompt = "Selecciona la reparacion.",
                    options = new[]
                    {
                        new MinigameOption { id = "params", label = "Consultas parametrizadas", isCorrect = true },
                        new MinigameOption { id = "reboot", label = "Reiniciar el servidor", isDistractor = true },
                        new MinigameOption { id = "pass", label = "Cambiar clave de administrador", isDistractor = true }
                    },
                    wrongFeedback = "Reiniciar o cambiar la clave no elimina la concatenacion de entrada."
                }
            },
            new IncidentStep
            {
                stepId = "verify",
                objectiveType = ObjectiveType.Verify,
                stage = IncidentStage.Verification,
                authorizedRole = RoleType.Pentester,
                objective = "Verificar la reparacion antes de restaurar el servicio.",
                briefing = explanation,
                successPoints = 10,
                completesIncident = true,
                successFeedback = "Verificado: entrada sospechosa tratada como datos y acceso legitimo correcto."
            }
        };
    }
}
