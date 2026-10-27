using System;
using UnityEngine;

[CreateAssetMenu(menuName = "CyberDefense/Incident definition")]
public class IncidentDefinition : ScriptableObject
{
    public string incidentId = "SQLI-001";
    public string title = "Acceso anomalo al portal de matriculas";

    [Header("Compatibilidad (Tarea 03)")]
    [Min(0)] public int wrongReportPenalty = 10;
    [Min(0)] public float wrongReportDelay = 5;
    [Min(1)] public float interactionRadius = 4;
    [TextArea(5, 12)] public string evidence =
        "[09:41:02] Portal de matriculas: solicitud de inicio de sesion.\n" +
        "[09:41:02] Campo usuario contiene comillas y una condicion logica inesperada.\n" +
        "[09:41:02] Traza de la aplicacion: entrada del formulario concatenada en una consulta SQL.\n" +
        "[09:41:03] La consulta devuelve varias cuentas; el usuario no tenia una sesion valida.\n" +
        "[09:41:04] No hay una secuencia masiva de intentos ni correos sospechosos.\n\n" +
        "Evidencia simulada: identifica que frontera entre datos e instrucciones se ha perdido.";
    [TextArea(4, 10)] public string explanation =
        "La aplicacion concatenaba datos del usuario con instrucciones SQL. Esto permitia alterar la consulta. " +
        "Las consultas parametrizadas separan instrucciones y valores: la entrada se trata como datos. " +
        "Reiniciar o cambiar una clave no elimina esta causa. La contencion limita el impacto, pero no sustituye la reparacion. " +
        "La verificacion simulo una entrada sospechosa rechazada y un acceso legitimo correcto. " +
        "El Pentester aporto evidencias y verifico; el Administrador contuvo y reparo. Todo el ejercicio es simulado.";

    [Header("Flujo por pasos (Tarea 03.5)")]
    [Tooltip("Pasos ordenados del incidente. Si esta vacio se usa el flujo SQL heredado.")]
    public IncidentStep[] steps = Array.Empty<IncidentStep>();
    public ScoringConfig scoring = new ScoringConfig();

    public bool HasSteps => steps != null && steps.Length > 0;
}
