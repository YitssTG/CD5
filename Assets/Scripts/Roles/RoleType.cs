/// <summary>
/// Identificadores de rol para la experiencia cooperativa de CyberDefense.
/// Vive en el espacio de nombres global para ser coherente con el resto de
/// scripts de gameplay (PlayerRole, ServidorRed, ConsolaControl, ZonaInteraccion).
/// Los valores numericos se mantienen alineados con los prefabs existentes:
/// SinAsignar = 0, Admin = 1, Pentester = 2.
/// </summary>
public enum RoleType
{
    SinAsignar = 0,
    Admin = 1,
    Pentester = 2
}
