using Fusion;
using UnityEngine;

public class ServidorRed : NetworkBehaviour
{
    public enum EstadoServidor { Normal, Hackeado, Reparado }

    [Networked]
    public EstadoServidor EstadoActual { get; set; } = EstadoServidor.Normal;

    [Networked]
    public int CodigoMitigacion { get; set; }

    private MeshRenderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
    }

    public override void Spawned()
    {
        ActualizarColorVisual();
    }

    public override void Render()
    {
        ActualizarColorVisual();
    }

    // --- SOLUCIÓN ERROR 1: Usamos RPC para que todos vean los cambios ---
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RecibirAtaquePentester(RpcInfo info = default)
    {
        if (!CanAct(info.Source, RoleType.Pentester)) return;

        if (EstadoActual == EstadoServidor.Normal)
        {
            CodigoMitigacion = Random.Range(1000, 9999);
            EstadoActual = EstadoServidor.Hackeado;
            Debug.Log($"<color=red>⚠️ ATAQUE EXITOSO:</color> El servidor está comprometido.");
            Debug.Log($"<color=yellow>🔑 CÓDIGO DE MITIGACIÓN:</color> {CodigoMitigacion}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_IntentarReparar(int codigoIngresado, RpcInfo info = default)
    {
        if (!CanAct(info.Source, RoleType.Admin)) return;

        if (EstadoActual == EstadoServidor.Hackeado)
        {
            if (codigoIngresado == CodigoMitigacion)
            {
                EstadoActual = EstadoServidor.Reparado;
                Debug.Log("<color=green>🛡️ SISTEMA RESTAURADO:</color> El parche ha sido aplicado.");
            }
            else
            {
                Debug.Log("<color=orange>❌ ERROR DE PARCHE:</color> El código no coincide.");
            }
        }
    }

    private bool CanAct(PlayerRef sender, RoleType role)
    {
        // Keep the original prototype for other scenes; it cannot bypass the cooperative mission.
        if (CooperativeIncident.Instance != null) return false;
        var roles = RoleManager.Instance;
        return roles != null && roles.IsOnline && roles.GameStarted && roles.GetRole(sender) == role;
    }

    void ActualizarColorVisual()
    {
        if (_renderer == null) return;

        var mission = CooperativeIncident.Instance;
        if (mission != null && mission.IsOnline)
        {
            _renderer.material.color = mission.Stage == IncidentStage.Resolved ? Color.green :
                mission.Stage >= IncidentStage.Contained ? Color.yellow :
                mission.Stage >= IncidentStage.Alert ? Color.red : Color.white;
            return;
        }

        switch (EstadoActual)
        {
            case EstadoServidor.Normal:
                _renderer.material.color = Color.white;
                break;
            case EstadoServidor.Hackeado:
                _renderer.material.color = Color.red;
                break;
            case EstadoServidor.Reparado:
                _renderer.material.color = Color.green;
                break;
        }
    }
}
