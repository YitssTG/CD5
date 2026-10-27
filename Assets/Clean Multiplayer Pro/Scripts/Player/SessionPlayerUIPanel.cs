#if CMPSETUP_COMPLETE
using System;
using System.Collections.Generic;
using AvocadoShark;
using UnityEngine;

public class SessionPlayerUIPanel : MonoBehaviour {

    public GameObject playerPortraitPrefab;

    public SessionPlayers sessionPlayersRef;

    public Action<bool> OnToggleButtonAfterPlayersChange;

    public Dictionary<PlayerStats ,PlayerUIEntry> playerUIEntryList = new Dictionary<PlayerStats, PlayerUIEntry>();  

    private void Awake() {
        sessionPlayersRef.OnPlayerAddedToSession += AddNewPlayerPortrait;
        sessionPlayersRef.OnPlayerRemovedFromSession += RemovePlayerPortrait;
        sessionPlayersRef.OnPlayerAddedToSession += ToggleVoteKickButton;
        sessionPlayersRef.OnPlayerRemovedFromSession += ToggleVoteKickButton;
    }

    public void AddNewPlayerPortrait(PlayerStats playerStats) {
        if (playerStats == null || playerUIEntryList.ContainsKey(playerStats)) return;
        GameObject portraitInstance = Instantiate(playerPortraitPrefab, transform);
        PlayerUIEntry playerUIEntry = portraitInstance.GetComponent<PlayerUIEntry>();
        playerUIEntryList.Add(playerStats, playerUIEntry);
        var isLocal = playerStats.Object.StateAuthority == playerStats.Runner.LocalPlayer;
        playerUIEntry.playerName =isLocal?$"<color=orange>{playerStats.PlayerName}</color>":playerStats.PlayerName.ToString();
        playerUIEntry.playerStats = playerStats;
        playerUIEntry.OnVoteKick += InitiateVoteKick;
        if (!playerStats.HasStateAuthority) { 
            OnToggleButtonAfterPlayersChange += playerUIEntry.ToggleVoteKickButton;
        }
    }

    public void ToggleVoteKickButton(PlayerStats playerStats) {
        if (SessionPlayers.instance.activePlayers.Count <= 2) {
            OnToggleButtonAfterPlayersChange?.Invoke(false);
        } else {
            OnToggleButtonAfterPlayersChange?.Invoke(true);
        }
    }

    public void RemovePlayerPortrait(PlayerStats playerStats) {
        if (!playerUIEntryList.TryGetValue(playerStats, out var entry)) return;
        if (entry != null) {
            OnToggleButtonAfterPlayersChange -= entry.ToggleVoteKickButton;
            entry.OnVoteKick -= InitiateVoteKick;
            Destroy(entry.gameObject);
        }
        playerUIEntryList.Remove(playerStats);
    }

    private void OnDestroy() {
        if (sessionPlayersRef != null) {
            sessionPlayersRef.OnPlayerAddedToSession -= AddNewPlayerPortrait;
            sessionPlayersRef.OnPlayerRemovedFromSession -= RemovePlayerPortrait;
            sessionPlayersRef.OnPlayerAddedToSession -= ToggleVoteKickButton;
            sessionPlayersRef.OnPlayerRemovedFromSession -= ToggleVoteKickButton;
        }
        OnToggleButtonAfterPlayersChange = null;
        playerUIEntryList.Clear();
    }

    public void InitiateVoteKick(PlayerStats playerStats) {
        playerStats.InitializeVoteKick();
    }


}
#endif
