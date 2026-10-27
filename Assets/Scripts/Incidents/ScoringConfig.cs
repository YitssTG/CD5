using System;
using UnityEngine;

/// <summary>
/// Gamificacion configurable de un incidente. Sin economia, tienda ni niveles.
/// </summary>
[Serializable]
public class ScoringConfig
{
    [Tooltip("Coste en puntos de revelar una pista.")]
    [Min(0)] public int hintCost = 3;
    [Tooltip("Bonus por completar un paso sin fallar.")]
    [Min(0)] public int accuracyBonus = 5;
    [Tooltip("Bonus cooperativo al completar el incidente.")]
    [Min(0)] public int cooperationBonus = 10;
    [Tooltip("Tope de penalizacion acumulada (0 = sin tope).")]
    [Min(0)] public int maxPenalty = 0;
}
