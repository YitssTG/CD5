using System;

/// <summary>
/// Reglas puras, sin dependencias de Unity ni Fusion. Evalua pasos de un
/// incidente a partir de la seleccion del jugador. CooperativeIncident valida
/// identidad, proximidad y revision ANTES de llamar aqui.
///
/// Entrada: choice + mask.
///  - SimpleChoice   (choice = indice).
///  - RequestAnalysis(mask    = conjunto de evidencias).
///  - ServerRepair   (choice  = secuencia empaquetada).
/// </summary>
public static class IncidentEngine
{
    public static int StepCount(IncidentStep[] steps)
    {
        return steps == null ? 0 : steps.Length;
    }

    public static bool TryGetStep(IncidentStep[] steps, int index, out IncidentStep step)
    {
        step = null;
        if (steps == null || index < 0 || index >= steps.Length) return false;
        step = steps[index];
        return step != null;
    }

    /// <summary>Etapa mostrada para el paso actual (Resolved cuando ya no hay pasos).</summary>
    public static IncidentStage StageFor(IncidentStep[] steps, int index)
    {
        int count = StepCount(steps);
        if (count == 0) return IncidentStage.Alert;
        if (index >= count) return IncidentStage.Resolved;
        if (index < 0) index = 0;
        IncidentStep step = steps[index];
        return step != null ? step.stage : IncidentStage.Alert;
    }

    public static bool TryEvaluate(IncidentStep[] steps, int index, RoleType role, int choice, int mask,
        out bool correct, out int pointsDelta, out IncidentFeedback feedback)
    {
        correct = false;
        pointsDelta = 0;
        feedback = IncidentFeedback.None;

        if (!TryGetStep(steps, index, out IncidentStep step))
        {
            feedback = IncidentFeedback.NotAllowed;
            return false;
        }
        if (role != step.authorizedRole)
        {
            feedback = IncidentFeedback.NotAllowed;
            return false;
        }
        if (!step.HasMinigame)
        {
            correct = true;
            pointsDelta = step.successPoints;
            feedback = IncidentFeedback.StepComplete;
            return true;
        }

        MinigameDefinition def = step.minigame;
        if (!def.HasOptions)
        {
            feedback = IncidentFeedback.NotAllowed;
            return false;
        }

        if (!TryResolveAnswer(def, choice, mask, out correct))
        {
            feedback = IncidentFeedback.NotAllowed;
            return false;
        }

        if (correct)
        {
            pointsDelta = step.successPoints;
            feedback = IncidentFeedback.StepComplete;
        }
        else
        {
            pointsDelta = -Math.Max(0, step.penaltyPoints);
            feedback = IncidentFeedback.StepFailed;
        }
        return true;
    }

    /// <summary>Traduce la respuesta entrante a correcto/incorrecto segun el tipo de minijuego.</summary>
    private static bool TryResolveAnswer(MinigameDefinition def, int choice, int mask, out bool correct)
    {
        correct = false;
        MinigameOption[] options = def.options;
        if (def.IsMultiSelect)
        {
            int want = MinigameSelection.CorrectMask(def);
            if (want == 0) return false;
            int stray = mask & ~((1 << options.Length) - 1);
            if (stray != 0) return false;
            correct = mask == want;
            return true;
        }

        if (def.IsSequence)
        {
            int[] order = def.EffectiveOrder();
            if (order == null || order.Length == 0) return false;
            correct = MinigameSelection.MatchesOrder(choice, order);
            return true;
        }

        int idx = choice >= 0 ? choice : MinigameSelection.FirstBit(mask);
        if (idx < 0 || idx >= options.Length || options[idx] == null) return false;
        correct = options[idx].isCorrect;
        return true;
    }

    /// <summary>Paso siguiente tras completar con exito, o StepCount si era el ultimo.</summary>
    public static int NextStep(IncidentStep[] steps, int index, IncidentStep step)
    {
        int count = StepCount(steps);
        if (step != null && (step.completesIncident || index >= count - 1)) return count;
        return index + 1;
    }
}
