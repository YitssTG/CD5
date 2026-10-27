using System;
using UnityEngine;

/// <summary>
/// Que mecanica de interaccion usa un paso.
/// El contrato de entrada de cada una se resuelve en MinigameSelection y
/// IncidentEngine; aqui solo vive la configuracion (Inspector).
/// </summary>
public enum MinigameKind
{
    None = 0,
    SimpleChoice = 1,
    RequestAnalysis = 2,
    ServerRepair = 3
}

/// <summary>Una opcion/evidencia/mini-recurso seleccionable dentro de un minijuego.</summary>
[Serializable]
public class MinigameOption
{
    [Tooltip("Identificador estable (para networking/pruebas).")]
    public string id = "";
    public string label = "";
    [Tooltip("Si es una respuesta/evidencia valida para completar el paso.")]
    public bool isCorrect = false;
    [Tooltip("Si es un distractor disenado a proposito (feedback educativo).")]
    public bool isDistractor = false;
    [TextArea(1, 3)] public string detail = "";
}

/// <summary>
/// Configuracion estatica de un minijuego. Vive dentro de un IncidentStep
/// (antes que como asset independiente) para reducir el numero de assets.
/// </summary>
[Serializable]
public class MinigameDefinition
{
    public MinigameKind kind = MinigameKind.None;
    [Range(0, 3)] public int difficulty = 0;
    [Tooltip("0 = intentos ilimitados.")]
    public int maxAttempts = 0;
    [Tooltip("0 = sin limite de tiempo (segundos).")]
    public float timeLimit = 0f;
    public string prompt = "";
    public MinigameOption[] options = Array.Empty<MinigameOption>();
    [Tooltip("Solo ServerRepair: orden en que hay que aplicar los recursos (indices de options).")]
    public int[] requiredOrder = Array.Empty<int>();
    [TextArea(1, 3)] public string correctFeedback = "";
    [TextArea(1, 3)] public string wrongFeedback = "";

    public bool HasOptions => options != null && options.Length > 0;

    public bool IsMultiSelect => kind == MinigameKind.RequestAnalysis;
    public bool IsSequence => kind == MinigameKind.ServerRepair;

    public int CountCorrect()
    {
        int total = 0;
        if (options == null) return 0;
        for (int i = 0; i < options.Length; i++)
            if (options[i] != null && options[i].isCorrect) total++;
        return total;
    }

    /// <summary>
    /// Orden exigido en ServerRepair: el declarado si existe; si no, las opciones
    /// correctas por orden ascendente (coincide con MinigameSelection.RequiredLength).
    /// </summary>
    public int[] EffectiveOrder()
    {
        if (requiredOrder != null && requiredOrder.Length > 0) return requiredOrder;
        if (options == null || options.Length == 0) return Array.Empty<int>();
        int count = CountCorrect();
        if (count <= 0) return Array.Empty<int>();
        var order = new int[count];
        int next = 0;
        for (int i = 0; i < options.Length; i++)
            if (options[i] != null && options[i].isCorrect) order[next++] = i;
        return order;
    }
}
