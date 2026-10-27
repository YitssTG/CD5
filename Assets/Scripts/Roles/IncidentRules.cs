// Enumeraciones compartidas del sistema de incidentes.
// La logica de decision vive en IncidentEngine (Assembly-CSharp::Incidents).
public enum IncidentStage { Waiting, Alert, Investigating, Diagnosed, Reported, Contained, Verification, Resolved }
public enum IncidentAction { Investigate, Diagnose, Report, Contain, Repair, Verify }
public enum IncidentFeedback
{
    None,
    Evidence,
    Diagnosis,
    WrongReport,
    ReportAccepted,
    WrongSystem,
    Contained,
    WrongRepair,
    RepairApplied,
    Verified,
    StepComplete,
    StepFailed,
    NotAllowed
}
