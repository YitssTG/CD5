#if CMPSETUP_COMPLETE
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using StarterAssets;
using Fusion.Photon.Realtime;
using UnityEngine.Serialization;
using System.Threading.Tasks;

namespace AvocadoShark
{
    public class FusionConnection : MonoBehaviour, INetworkRunnerCallbacks
    {
        #region ThingsAddedByMe

        public List<PlayerRef> playersInSession = new List<PlayerRef>();

        #endregion

        public RoomEntry CurrentEntryBeingEdited;
        public Action OnDestroyRoomEntries;

        public static FusionConnection Instance;

        [SerializeField] private NetworkRunner runnerPrefab;
        public NetworkRunner Runner { get; private set; }

        public bool hasEnteredGameScene = false;

        [Header("Player 1")] [SerializeField] public GameObject playerPrefabYellow;
        [Header("Player 2")] [SerializeField] public GameObject playerPrefabRed;
        public CharacterSO characterScriptableObject;
        [Header("Name Entry")] public GameObject mainObject;
        public Button submitButton;
        public TMP_InputField nameField;
        public GameObject characterselectionobject;

        [Header("Room List")] public RoomEntry roomEntryPrefab;
        public GameObject roomListObject;
        public Transform content;
        public Button createRoomButton;
        public TextMeshProUGUI NoRoomsText;
        public TMP_InputField room_search;

        [Header("Room List Refresh (s)")] [SerializeField]
        private float refreshInterval = 2f;

        [Header("Player Spawn Location")] [SerializeField]
        public bool UseCustomLocation;

        [SerializeField] public Vector3 CustomLocation;

        private FusionVoiceClient _fvc;
        private Recorder _recorder;
        private VoiceManager _voiceManager;

        [Header("Loading Screen")] public LoadingScreen loadingScreenScript;

        [Header("Loading Screen")] public PopUp popup;

        [Header("UI")] [SerializeField] private MenuCanvas menuCanvas;

        private bool initialRoomListPopulated = false;
        private List<SessionInfo> _sessionList = new List<SessionInfo>();
        private List<RoomEntry> _roomEntryList = new List<RoomEntry>();

        [HideInInspector] public bool isConnected = false;
        [HideInInspector] public string _playerName = null;
        [HideInInspector] public int nRooms = 0;
        [HideInInspector] public int nPPLOnline = 0;

        public TMP_Dropdown region_select;
        public Button backButton;

        private bool _operationInProgress;
        private bool _returningToMenu;
        private bool _quitting;
        private Task _returnToMenuTask;
        private static string _pendingError;
        private const string MenuScenePath = "Assets/Clean Multiplayer Pro/Scenes/Menu.unity";
        private const string GameScenePath = "Assets/Clean Multiplayer Pro/Scenes/Game.unity";
        private const string EnvironmentScenePath = "Assets/Clean Multiplayer Pro/Scenes/Environment 1.unity";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            room_search.onValueChanged.AddListener(OnSearchTextValueChange);
#if UNITY_2022_3_OR_NEWER
        Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
#else
            Application.targetFrameRate = Screen.currentResolution.refreshRate;
#endif

            int region_n = PlayerPrefs.GetInt("region");
            string region = null;
            if (region_n == 0)
            {
                region = "";
            }
            else if (region_n == 1)
            {
                region = "asia";
            }
            else if (region_n == 2)
            {
                region = "eu";
            }
            else if (region_n == 3)
            {
                region = "jp";
            }
            else if (region_n == 4)
            {
                region = "kr";
            }
            else if (region_n == 5)
            {
                region = "us";
            }
            region_select.value = region_n;
            PhotonAppSettings settings = Resources.Load<PhotonAppSettings>("PhotonAppSettings");
            settings.AppSettings.FixedRegion = region;
        }

        private void Start()
        {
            if (Instance != this) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (!string.IsNullOrEmpty(_pendingError))
            {
                popup.ShowPopup(_pendingError);
                _pendingError = null;
            }
        }

        private void Update()
        {
            // Scene callbacks can run before Fusion finishes the entire scene queue.
            if (_returningToMenu || hasEnteredGameScene || Runner == null || !Runner.IsRunning || Runner.IsSceneManagerBusy)
                return;
            if (!SceneManager.GetSceneByPath(GameScenePath).isLoaded ||
                !SceneManager.GetSceneByPath(EnvironmentScenePath).isLoaded)
                return;
            hasEnteredGameScene = true;
            try { SpawnPlayerCharacter(Runner); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ReturnToMenu(exception.Message);
            }
        }

        private void OnApplicationQuit() { _quitting = true; }

        private void OnDestroy()
        {
            if (room_search != null) room_search.onValueChanged.RemoveListener(OnSearchTextValueChange);
            if (Runner != null) Runner.RemoveCallbacks(this);
            if (Instance == this) Instance = null;
        }

        public async void ReturnToMenu(string error = null)
        {
            try { await ReturnToMenuAsync(error); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        public Task ReturnToMenuAsync(string error = null)
        {
            if (_returnToMenuTask != null) return _returnToMenuTask;
            _returningToMenu = true;
            _pendingError = error;
            return _returnToMenuTask = CloseSessionAndLoadMenu();
        }

        private async Task CloseSessionAndLoadMenu()
        {
            CancelInvoke();
            StopAllCoroutines();
            // Leave the callback stack before asking Fusion to finish shutdown.
            await Task.Yield();
            var oldRunner = Runner;
            if (oldRunner != null)
            {
                await oldRunner.Shutdown();
                if (oldRunner != null)
                {
                    oldRunner.RemoveCallbacks(this);
                    Destroy(oldRunner.gameObject);
                }
            }
            Runner = null;
            playersInSession.Clear();
            _roomEntryList.Clear();
            _sessionList.Clear();
            CurrentEntryBeingEdited = null;
            OnDestroyRoomEntries = null;
            isConnected = false;
            hasEnteredGameScene = false;
            if (_quitting || !Application.isPlaying) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Instance = null;
            Destroy(gameObject);
            // A fresh scene supplies fresh UI references and a fresh session controller.
            var load = SceneManager.LoadSceneAsync(MenuScenePath, LoadSceneMode.Single);
            while (!load.isDone) await Task.Yield();
        }

        public void ChangeRegion(int regionNum)
        {
            string region = null;
            var settings = Resources.Load<PhotonAppSettings>("PhotonAppSettings");
            region = regionNum switch
            {
                0 => "",
                1 => "asia",
                2 => "eu",
                3 => "jp",
                4 => "kr",
                5 => "us",
                _ => null
            };
            settings.AppSettings.FixedRegion = region;
            PlayerPrefs.SetInt("region", regionNum);
        }
        public void RefreshRoomList()
        {
            InitialRoomListSetup();
        }
        
        private IEnumerator AutoRefreshRoomList()
        {
            while (true)
            {
                RefreshRoomList();
                yield return new WaitForSeconds(refreshInterval);
            }
        }

        public void OnSearchTextValueChange(string value)
        {
            _roomEntryList.RemoveAll(entry => entry == null);
            foreach (var i in _roomEntryList)
            {
                i.gameObject.SetActive(string.IsNullOrWhiteSpace(value) || i.roomName.text.Contains(value));
            }
        }

        public void CreateRoom()
        {
            if (_operationInProgress || _returningToMenu || IsInvoking(nameof(ContinueCreateRoom))) return;
            PlayerPrefs.SetInt("has_pass", 0);
            loadingScreenScript.gameObject.SetActive(true);
            Invoke(nameof(ContinueCreateRoom), loadingScreenScript.lerpSpeed);
        }
        private void ContinueCreateRoom()
        {
            string sessionName = null;
            string sessionPassword = null;
            int maxPlayers = 2;
            if (IsRoomNameValid())
            {
                sessionName = menuCanvas.GetRoomName();
                sessionPassword = menuCanvas.GetPassword();
                maxPlayers = menuCanvas.GetMaxPlayers();
            }
            else
            {
                int randomInt = UnityEngine.Random.Range(1000, 9999);
                sessionPassword = menuCanvas.GetPassword();
                maxPlayers = menuCanvas.GetMaxPlayers();
                sessionName = "Room-" + randomInt;
            }

            Debug.Log($"Session name is {sessionName}");

            // La experiencia cooperativa es para exactamente dos jugadores.
            maxPlayers = 2;

            Debug.Log($"maxPlayers is {maxPlayers}");

            if (menuCanvas.IsPasswordEnabled)
            {
                PlayerPrefs.SetInt("has_pass", 1);
                JoinRoom(sessionName, maxPlayers, sessionPassword);
            }
            else
            {
                JoinRoom(sessionName, maxPlayers,string.Empty);
            }

            StopCoroutine(AutoRefreshRoomList());
        }

        private bool IsRoomNameValid()
        {
            return menuCanvas.GetRoomName().Length != 0;
        }

        public bool TryGetLocalPlayerComponent<T>(out T component)
        {
            component = default;
            if (_returningToMenu || Runner == null || !Runner.IsRunning) return false;
            var localPlayer = Runner.GetPlayerObject(Runner.LocalPlayer);
            if (localPlayer == null || !localPlayer.IsValid || !localPlayer.HasStateAuthority ||
                localPlayer.StateAuthority != Runner.LocalPlayer)
            {
                component = default;
                return false;
            }
            component = localPlayer.GetComponent<T>();
            return component != null;
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            if (runner != Runner || _returningToMenu || roomListObject == null) return;
            if (initialRoomListPopulated == false)
            {
                //StartCoroutine(AutoRefreshRoomList());
                loadingScreenScript.FadeOutAndDisable();
            }

            _sessionList = sessionList;
            nRooms = sessionList.Count;

            nPPLOnline = 0;
            foreach (var session in sessionList)
            {
                nPPLOnline += session.PlayerCount;
            }

            RefreshRoomList();
        }

        private void InitialRoomListSetup()
        {
            if (roomListObject == null)
                return;
            initialRoomListPopulated = true;
            roomListObject.SetActive(true);

            // Keep only the row whose password is being edited, if its room still exists.
            if (CurrentEntryBeingEdited != null &&
                !_sessionList.Exists(session => session.Name == CurrentEntryBeingEdited.sessionInfo.Name))
            {
                CurrentEntryBeingEdited.DestroyEntry();
                CurrentEntryBeingEdited = null;
            }
            OnDestroyRoomEntries?.Invoke();
            _roomEntryList.Clear();
            if (CurrentEntryBeingEdited != null) _roomEntryList.Add(CurrentEntryBeingEdited);

            foreach (SessionInfo session in _sessionList)
            {
                if (CurrentEntryBeingEdited != null)
                {
                    if (session.Name == CurrentEntryBeingEdited.sessionInfo.Name)
                    {
                        UpdateCurrentEntryBeingEdited(session);
                        continue;
                    }
                }

                RoomEntry entryScript = Instantiate(roomEntryPrefab, content);
                entryScript.Init(session,this);
                _roomEntryList.Add(entryScript);
            }

            if (_sessionList.Count == 0)
            {
                NoRoomsText.gameObject.SetActive(true);
            }
            else
            {
                NoRoomsText.gameObject.SetActive(false);
            }
            OnSearchTextValueChange(room_search.text);
        }

        //public void AddPlayerToBannedList(string name) {
        //runner.SessionInfo.UpdateCustomProperties();
        //}

        public void ConnectToRunner()
        {
            if (_operationInProgress || _returningToMenu || Runner != null || IsInvoking(nameof(ContinueConnectToRunner))) return;
            loadingScreenScript.gameObject.SetActive(true);
            Invoke(nameof(ContinueConnectToRunner), loadingScreenScript.lerpSpeed);
        }
        private async void ContinueConnectToRunner()
        {
            if (_operationInProgress || _returningToMenu) return;
            _operationInProgress = true;
            _playerName = nameField.text;
            mainObject.SetActive(false);
            characterselectionobject.SetActive(false);
            try
            {
                SetUpComponents();
                var result = await Runner.JoinSessionLobby(SessionLobby.Shared);
                if (!result.Ok && !_returningToMenu) ReturnToMenu(result.ShutdownReason.ToString());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (!_returningToMenu) ReturnToMenu(exception.Message);
            }
            finally { _operationInProgress = false; }
        }
        private void SetUpComponents()
        {
            if (Runner != null) return;
            Runner = Instantiate(runnerPrefab);
            DontDestroyOnLoad(Runner.gameObject);
            if (Runner.GetComponent<INetworkSceneManager>() == null)
                Runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            if (Runner.GetComponent<INetworkObjectProvider>() == null)
                Runner.gameObject.AddComponent<NetworkObjectProviderDefault>();
            _fvc = Runner.GetComponent<FusionVoiceClient>();
            _recorder = Runner.GetComponentInChildren<Recorder>();
            _voiceManager = Runner.GetComponentInChildren<VoiceManager>();
            Runner.AddCallbacks(this);
        }

        public void JoinRoom(string sessionName, string password = null)
        {
            StartSession(sessionName, null, password);
        }

        public void JoinRoom(string sessionName, int maxPlayers, string password = null)
        {
            StartSession(sessionName, maxPlayers, password);
        }

        private async void StartSession(string sessionName, int? maxPlayers, string password)
        {
            if (_operationInProgress || _returningToMenu || (Runner != null && Runner.IsRunning)) return;
            _operationInProgress = true;
            try
            {
                int gameIndex = SceneUtility.GetBuildIndexByScenePath(GameScenePath);
                int environmentIndex = SceneUtility.GetBuildIndexByScenePath(EnvironmentScenePath);
                if (gameIndex < 0 || environmentIndex < 0)
                    throw new InvalidOperationException("Game y Environment 1 deben estar habilitadas en Build Settings.");
                SetUpComponents();
                var scenes = new NetworkSceneInfo();
                scenes.AddSceneRef(SceneRef.FromIndex(gameIndex), LoadSceneMode.Single);
                scenes.AddSceneRef(SceneRef.FromIndex(environmentIndex), LoadSceneMode.Additive, activeOnLoad: false);
                var result = await Runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = sessionName,
                    Scene = scenes,
                    SceneManager = Runner.GetComponent<INetworkSceneManager>(),
                    ObjectProvider = Runner.GetComponent<INetworkObjectProvider>(),
                    PlayerCount = maxPlayers,
                    SessionProperties = maxPlayers.HasValue
                        ? new Dictionary<string, SessionProperty> { { "password", password ?? string.Empty } }
                        : null
                });
                if (!result.Ok && !_returningToMenu) ReturnToMenu(result.ShutdownReason.ToString());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (!_returningToMenu) ReturnToMenu(exception.Message);
            }
            finally { _operationInProgress = false; }
        }

        private void SpawnPlayerCharacter(NetworkRunner runner)
        {
            if (runner.GetPlayerObject(runner.LocalPlayer) != null)
                return;
            var playerPrefab = characterScriptableObject.GetSelectedCharacter().character;
            var location = !UseCustomLocation
                ? new Vector3(UnityEngine.Random.Range(-7.6f, 14.2f), 0,
                    UnityEngine.Random.Range(-31.48f, -41.22f))
                : CustomLocation;
            var playerObject = runner.Spawn(playerPrefab,location);
            runner.SetPlayerObject(runner.LocalPlayer, playerObject.Object);
            Debug.Log($"[LocalControl] registered local={runner.LocalPlayer} avatar={playerObject.Object.Id} owner={playerObject.Object.StateAuthority}");
            //playerObject.transform.position = location;
            

            _voiceManager.Init(playerObject.GetComponent<StarterAssetsInputs>(),
                playerObject.GetComponent<PlayerWorldUIManager>());


        }

        #region INetworkCallbacks
        
        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log("OnConnectedToServer");
            isConnected = true;
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            isConnected = false;
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            if (!_quitting && !_returningToMenu && runner == Runner) ReturnToMenu(reason.ToString());
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
            byte[] token)
        {
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key,
            ArraySegment<byte> data)
        {
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner == Runner && !playersInSession.Contains(player)) playersInSession.Add(player);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner != Runner) return;
            playersInSession.RemoveAll(item => item == player);
            if (SessionPlayers.instance != null) SessionPlayers.instance.RemovePlayer(player);
        }
        public void OnSceneLoadDone(NetworkRunner runner)
        {
            // Both initial scenes are synchronized by NetworkSceneInfo.
            // Update spawns once the full queue has completed on this peer.
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (!_quitting && !_returningToMenu && runner == Runner)
                ReturnToMenu(shutdownReason == ShutdownReason.Ok ? null : shutdownReason.ToString());
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }
        #endregion
        public void Checkforname()
        {
            if (nameField.text != "")
                submitButton.interactable = true;
            else
                submitButton.interactable = false;
        }

        public void UpdateCurrentEntryBeingEdited(SessionInfo session)
        {
            CurrentEntryBeingEdited.UpdateEntry(session,this);
        }
        public void SetCurrentEntryBeingEdited(RoomEntry roomEntry)
        {
            if (CurrentEntryBeingEdited == roomEntry) return;
            if (CurrentEntryBeingEdited != null)
            {
                // If we were previously editing an entry
                OnDestroyRoomEntries += CurrentEntryBeingEdited.DestroyEntry;
                CurrentEntryBeingEdited = roomEntry;
            }
            else
            {
                // If we were not editing any entry
                CurrentEntryBeingEdited = roomEntry;
            }
        }
        public void ResetCurrentEntryBeingEdited(RoomEntry roomEntry)
        {
            if (roomEntry == CurrentEntryBeingEdited)
            {
                OnDestroyRoomEntries += CurrentEntryBeingEdited.DestroyEntry;
                CurrentEntryBeingEdited = null;
            }
        }
    }
}
#endif
