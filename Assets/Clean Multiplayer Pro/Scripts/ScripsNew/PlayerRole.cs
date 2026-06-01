using Fusion;
using UnityEngine;
using StarterAssets;

public class PlayerRole : NetworkBehaviour
{
    public enum RoleType { SinAsignar, Admin, Pentester }

    [Header("Configuración de Rol")]
    public RoleType rolInicial;
    [Networked] public RoleType MyRole { get; set; }

    private StarterAssetsInputs _input;

    private void Awake()
    {
        Debug.Log("🟡 PlayerRole Awake en: " + gameObject.name);
    }

    public override void Spawned()
    {
        Debug.Log("🟢 PlayerRole Spawned!");
        if (Object.HasStateAuthority) MyRole = rolInicial;
        _input = GetComponent<StarterAssetsInputs>();
        gameObject.name = "Player_" + MyRole.ToString();
    }

    // Llamado desde ZonaInteraccion
    public void InteractuarCon(GameObject objeto)
    {
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