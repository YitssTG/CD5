using Fusion;

public static class MissionNetworkBudget
{
    public const int EventCapacity = 64;
    public const int TicketCapacity = 16;
    public const int EventTextCapacity = 64;
}

public enum MissionPhase { Waiting, Tutorial, Active, Complete, Interrupted }
public enum TicketStatus { None, Submitted, Returned, Accepted }
public enum MissionEventKind { TeamReady, TutorialComplete, Started, OperationStarted, OperationCancelled,
    Evidence, TicketSent, TicketReceived, TicketReturned, TicketAccepted, Contained, Repaired, Verified,
    Resolved, Error, Hint, Disconnected, Interrupted }

public struct MissionRecord : INetworkStruct
{
    public int Sequence;
    public float Seconds;
    public PlayerRef Actor;
    public RoleType Role;
    public MissionEventKind Kind;
    public int Points;
    public NetworkString<_64> Detail;
}

public struct IncidentTicket : INetworkStruct
{
    public int Version;
    public float Seconds;
    public PlayerRef Author;
    public int Diagnosis;
    public int EvidenceMask;
    public int Severity;
    public int Recommendation;
    public int System;
    public NetworkString<_128> Comment;
}

public static class TicketRules
{
    public const int CommentLimit = 128;
    // UTF-8 RPC strings avoid sending the entire 516-byte NetworkString<_128> storage.
    public static string NormalizeComment(string value)
    {
        value = value ?? "";
        if (value.Length <= CommentLimit) return value;
        int length = CommentLimit;
        if (char.IsHighSurrogate(value[length - 1])) length--;
        return value.Substring(0, length);
    }
    public static bool IsValid(int diagnosis, int mask, int severity, int recommendation, int system) =>
        diagnosis >= 0 && diagnosis < 3 && mask > 0 && (mask & ~15) == 0 && severity >= 0 && severity < 3 &&
        recommendation >= 0 && recommendation < 3 && system >= 0 && system < 2;
    public static bool IsCorrect(int diagnosis, int mask, int severity, int recommendation, int system) =>
        diagnosis == 0 && mask == 7 && severity == 2 && recommendation == 0 && system == 0;
    public static string Code(int incident, int version) => $"TK-{incident:000}-{version:000}";
    public static string DiagnosisName(int i) => i == 0 ? "SQL Injection" : i == 1 ? "Fuerza bruta" : "Phishing";
    public static string SeverityName(int i) => i == 0 ? "Baja" : i == 1 ? "Media" : "Alta";
    public static string RecommendationName(int i) => i == 0 ? "Consultas parametrizadas" : i == 1 ? "Reiniciar servidor" : "Cambiar contrasena";
    public static string EvidenceNames(int mask)
    {
        string[] labels = { "entrada concatenada", "consulta alterada", "acceso no autorizado", "correo no observado" };
        var selected = new System.Collections.Generic.List<string>();
        for (int i = 0; i < labels.Length; i++) if ((mask & (1 << i)) != 0) selected.Add(labels[i]);
        return string.Join(", ", selected);
    }
}
