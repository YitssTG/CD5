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
    public void RPC_RecibirAtaquePentester()
    {
        if (EstadoActual == EstadoServidor.Normal)
        {
            CodigoMitigacion = Random.Range(1000, 9999);
            EstadoActual = EstadoServidor.Hackeado;
            Debug.Log($"<color=red>⚠️ ATAQUE EXITOSO:</color> El servidor está comprometido.");
            Debug.Log($"<color=yellow>🔑 CÓDIGO DE MITIGACIÓN:</color> {CodigoMitigacion}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_IntentarReparar(int codigoIngresado)
    {
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

    void ActualizarColorVisual()
    {
        if (_renderer == null) return;

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