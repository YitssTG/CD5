using Fusion;
using UnityEngine;
using AvocadoShark;

/// <summary>
/// Arbitro central del sistema de roles cooperativos.
///
/// Se coloca en un NetworkObject de escena del Game. En Fusion Shared Mode el
/// SharedModeMasterClient tiene StateAuthority sobre los objetos de escena, por
/// lo que este componente es la unica fuente de verdad para asignar roles: todas
/// las peticiones llegan por RPC al StateAuthority y se resuelven en serie, lo
/// que evita selecciones duplicadas y condiciones de carrera.
///
/// La partida solo comienza (GameStarted = true) cuando hay un Pentester y un
/// Administrador distintos y ambos se han marcado como listos.
/// </summary>
public class RoleManager : NetworkBehaviour
{
    public static RoleManager Instance { get; private set; }
    private bool hasSpawned;

    [Header("Estado de la partida (solo lectura)")]
    [Networked] public PlayerRef AdminPlayer { get; set; }
    [Networked] public PlayerRef PentesterPlayer { get; set; }
    [Networked] public NetworkBool AdminReady { get; set; }
    [Networked] public NetworkBool PentesterReady { get; set; }
    [Networked] public NetworkBool GameStarted { get; set; }
    [Networked] public NetworkString<_64> StatusMessage { get; set; }

    /// <summary>Mensaje de error solo para el jugador local (no se sincroniza).</summary>
    public string LocalErrorMessage { get; private set; }

    // Helpers para que la interfaz no dependa directamente de la API interna de Fusion.
    public bool IsOnline => hasSpawned && Runner != null && Object != null && Object.IsValid;

    public PlayerRef LocalPlayerRef => Runner != null ? Runner.LocalPlayer : PlayerRef.None;

    public int ActivePlayerCount
    {
        get
        {
            if (Runner == null) return 0;
            int count = 0;
            foreach (var _ in Runner.ActivePlayers) count++;
            return count;
        }
    }

    public RoleType LocalRole => IsOnline ? GetRole(Runner.LocalPlayer) : RoleType.SinAsignar;

    public override void Spawned()
    {
        hasSpawned = true;
        Instance = this;
        if (HasStateAuthority)
        {
            AdminPlayer = PlayerRef.None;
            PentesterPlayer = PlayerRef.None;
            AdminReady = false;
            PentesterReady = false;
            GameStarted = false;
            StatusMessage = "Esperando a los jugadores...";
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        hasSpawned = false;
        if (Instance == this) Instance = null;
    }

    private void OnDestroy()
    {
        hasSpawned = false;
        if (Instance == this) Instance = null;
    }

    // Se ejecuta en el StateAuthority (SharedModeMasterClient) en cada tick de red.
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        bool released = false;
        if (AdminPlayer != PlayerRef.None && !IsActivePlayer(AdminPlayer))
        {
            AdminPlayer = PlayerRef.None;
            AdminReady = false;
            released = true;
        }
        if (PentesterPlayer != PlayerRef.None && !IsActivePlayer(PentesterPlayer))
        {
            PentesterPlayer = PlayerRef.None;
            PentesterReady = false;
            released = true;
        }
        if (released)
        {
            GameStarted = false;
            StatusMessage = "Un jugador se desconecto. Rol liberado.";
        }

        bool bothAssigned = AdminPlayer != PlayerRef.None && PentesterPlayer != PlayerRef.None;
        bool shouldStart = bothAssigned && AdminReady && PentesterReady;
        if (GameStarted != shouldStart)
        {
            GameStarted = shouldStart;
            StatusMessage = shouldStart
                ? "Ambos jugadores estan listos. Comienza la partida."
                : (bothAssigned ? "Pulsa LISTO para comenzar." : "Cada jugador debe elegir un rol.");
        }
    }

    // ---------------------------------------------------------------- API local

    public void RequestRole(RoleType role)
    {
        if (!IsOnline || (role != RoleType.Admin && role != RoleType.Pentester)) return;
        RPC_RequestRole(role);
    }

    public void RequestReady()
    {
        if (!IsOnline) return;
        RPC_SetReady();
    }

    public void RequestRelease()
    {
        if (!IsOnline) return;
        RPC_ReleaseRole();
    }

    public void ClearLocalError()
    {
        LocalErrorMessage = null;
    }

    public RoleType GetRole(PlayerRef player)
    {
        if (!IsOnline || player == PlayerRef.None) return RoleType.SinAsignar;
        if (player == AdminPlayer) return RoleType.Admin;
        if (player == PentesterPlayer) return RoleType.Pentester;
        return RoleType.SinAsignar;
    }

    public string GetRoleName(RoleType role)
    {
        switch (role)
        {
            case RoleType.Admin: return "Administrador";
            case RoleType.Pentester: return "Pentester";
            default: return "Sin asignar";
        }
    }

    // ----------------------------------------------------------------- RPCs

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestRole(RoleType role, RpcInfo info = default)
    {
        if (!HasStateAuthority) return;
        AssignRole(info.Source, role);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetReady(RpcInfo info = default)
    {
        if (!HasStateAuthority) return;
        PlayerRef sender = info.Source;
        if (!IsActivePlayer(sender)) return;
        if (sender != PlayerRef.None && sender == AdminPlayer)
        {
            AdminReady = true;
        }
        else if (sender != PlayerRef.None && sender == PentesterPlayer)
        {
            PentesterReady = true;
        }
        else
        {
            RPC_RoleRejected(sender, "Debes elegir un rol antes de marcarte listo.");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ReleaseRole(RpcInfo info = default)
    {
        if (!HasStateAuthority) return;
        PlayerRef sender = info.Source;
        if (!IsActivePlayer(sender)) return;
        bool changed = false;
        if (sender != PlayerRef.None && sender == AdminPlayer)
        {
            AdminPlayer = PlayerRef.None;
            AdminReady = false;
            changed = true;
        }
        if (sender != PlayerRef.None && sender == PentesterPlayer)
        {
            PentesterPlayer = PlayerRef.None;
            PentesterReady = false;
            changed = true;
        }
        if (changed)
        {
            GameStarted = false;
            StatusMessage = "Rol liberado.";
            RPC_RoleConfirmed(sender, RoleType.SinAsignar);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RoleConfirmed(PlayerRef player, RoleType role)
    {
        if (Runner != null && player == Runner.LocalPlayer) ApplyLocalRole(role);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RoleRejected(PlayerRef player, NetworkString<_64> message)
    {
        if (Runner != null && player == Runner.LocalPlayer) LocalErrorMessage = message.ToString();
    }

    // ------------------------------------------------------------- internos

    private void AssignRole(PlayerRef sender, RoleType role)
    {
        if (sender == PlayerRef.None || !IsActivePlayer(sender) ||
            (role != RoleType.Admin && role != RoleType.Pentester)) return;

        if (role == RoleType.Admin)
        {
            if (AdminPlayer != PlayerRef.None && AdminPlayer != sender)
            {
                RPC_RoleRejected(sender, "El rol de Administrador ya esta ocupado.");
                return;
            }
            if (PentesterPlayer == sender)
            {
                PentesterPlayer = PlayerRef.None;
                PentesterReady = false;
            }
            AdminPlayer = sender;
            AdminReady = false;
        }
        else if (role == RoleType.Pentester)
        {
            if (PentesterPlayer != PlayerRef.None && PentesterPlayer != sender)
            {
                RPC_RoleRejected(sender, "El rol de Pentester ya esta ocupado.");
                return;
            }
            if (AdminPlayer == sender)
            {
                AdminPlayer = PlayerRef.None;
                AdminReady = false;
            }
            PentesterPlayer = sender;
            PentesterReady = false;
        }

        GameStarted = false;
        RPC_RoleConfirmed(sender, role);
    }

    private void ApplyLocalRole(RoleType role)
    {
        if (FusionConnection.Instance != null &&
            FusionConnection.Instance.TryGetLocalPlayerComponent(out PlayerRole playerRole))
        {
            playerRole.SetRole(role);
        }
    }

    private bool IsActivePlayer(PlayerRef player)
    {
        foreach (var active in Runner.ActivePlayers)
        {
            if (active == player) return true;
        }
        return false;
    }
}
