using Fusion;
using UnityEngine;
using StarterAssets;

public class PlayerRole : NetworkBehaviour
{
    [Header("Configuración de Rol")]
    [Tooltip("Valor heredado. El rol se asigna en partida a través de RoleManager; este campo ya no se usa.")]
    public RoleType rolInicial = RoleType.SinAsignar;

    [Networked] public RoleType MyRole { get; set; }

    private StarterAssetsInputs _input;
    private RoleType _lastVisualRole = (RoleType)(-1);

    public bool IsRoleAssigned => MyRole != RoleType.SinAsignar;

    private void Awake()
    {
        Debug.Log("🟡 PlayerRole Awake en: " + gameObject.name);
    }

    public override void Spawned()
    {
        _input = GetComponent<StarterAssetsInputs>();

        if (Object.HasStateAuthority)
        {
            // El rol ya no se hereda del prefab: se decide en la fase de selección.
            // Si el RoleManager ya confirma un rol para este jugador (por ejemplo,
            // si se reconecta) lo aplicamos; si no, queda SinAsignar.
            MyRole = RoleType.SinAsignar;
            if (RoleManager.Instance != null && RoleManager.Instance.IsOnline)
                MyRole = RoleManager.Instance.GetRole(Object.StateAuthority);
        }

        UpdateIdentity();
    }

    public override void Render()
    {
        UpdateIdentity();
    }

    // Llamado por RoleManager cuando el arbitraje confirma el rol del jugador local.
    public void SetRole(RoleType role)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority) return;
        MyRole = role;
        UpdateIdentity();
    }

    private void UpdateIdentity()
    {
        if (_lastVisualRole == MyRole) return;
        _lastVisualRole = MyRole;
        gameObject.name = "Player_" + MyRole;
    }

    // Llamado desde ZonaInteraccion
    public void InteractuarCon(GameObject objeto)
    {
        if (RoleManager.Instance == null || !RoleManager.Instance.IsOnline || !RoleManager.Instance.GameStarted)
        {
            Debug.Log("⏳ La partida aún no ha comenzado. Selecciona un rol primero.");
            return;
        }

        if (MyRole == RoleType.SinAsignar)
        {
            Debug.Log("⚠️ No tienes un rol asignado.");
            return;
        }

        if (CooperativeIncident.Instance != null)
        {
            if (Object != null && Object.IsValid && Object.HasStateAuthority)
                RoleTerminalUI.OpenLocalTerminal();
            return;
        }

        Debug.Log($"🚀 InteractuarCon: {objeto.name} | Rol: {MyRole}");

        ServidorRed servidor = objeto.GetComponent<ServidorRed>();
        if (servidor != null)
        {
            if (MyRole == RoleType.Pentester)
            {
                Debug.Log("🎯 Pentester atacando servidor...");
                // Llamamos al RPC
                servidor.RPC_RecibirAtaquePentester();
            }
            else
            {
                Debug.Log("⛔ ADMIN: No puedes atacar el servidor.");
            }
            return;
        }

        ConsolaControl consola = objeto.GetComponent<ConsolaControl>();
        if (consola != null)
        {
            if (MyRole == RoleType.Admin)
            {
                if (consola.servidorAsociado == null)
                {
                    Debug.Log("⚠️ Consola sin servidor asignado.");
                    return;
                }

                if (consola.servidorAsociado.EstadoActual != ServidorRed.EstadoServidor.Hackeado)
                {
                    Debug.Log("ℹ️ No hay ataque activo. Espera el reporte del Pentester.");
                    return;
                }

                Debug.Log("🎯 Admin reparando servidor...");
                // Llamamos al RPC
                consola.servidorAsociado.RPC_IntentarReparar(consola.servidorAsociado.CodigoMitigacion);
            }
            else
            {
                Debug.Log("⛔ PENTESTER: Sin acceso a la terminal.");
            }
            return;
        }

        Debug.Log($"⚠️ '{objeto.name}' no tiene ServidorRed ni ConsolaControl");
    }
}
