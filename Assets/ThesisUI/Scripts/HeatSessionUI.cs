using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AvocadoShark;
using Michsky.UI.Heat;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace CyberDefense.UI
{
    // Scene-local adapter: Heat owns presentation; CMP/Fusion still owns sessions.
    public sealed class HeatSessionUI : MonoBehaviour
    {
        private const string MenuPath = "Assets/Clean Multiplayer Pro/Scenes/Menu.unity";
        private const string GamePath = "Assets/Clean Multiplayer Pro/Scenes/Game.unity";
        private const string HeatMenuPath = "Assets/ThesisUI/Scenes/Heat Main Menu.unity";
        private const string HeatPausePath = "Assets/ThesisUI/Scenes/Heat Pause Menu.unity";
        private static HeatSessionUI pauseUI;
        private readonly List<Canvas> lobbyCanvases = new List<Canvas>();
        private Scene ownerScene;
        private GameObject heatCanvas;
        private GameObject backCanvas;
        private PauseMenuManager pauseManager;
        private bool inGame;
        private bool pauseOpen;
        private bool switchingLobby;
        private CanvasGroup roster;
        private CanvasGroup legacyRoomControls;
        private bool rosterOpen;
        private GameObject fallbackPause;
        public static bool IsPaused => pauseUI != null && pauseUI.pauseOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            pauseUI = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != MenuPath && scene.path != GamePath) return;
            if (Components<HeatSessionUI>(scene).Any()) return;
            var root = new GameObject("Heat Session UI");
            SceneManager.MoveGameObjectToScene(root, scene);
            var adapter = root.AddComponent<HeatSessionUI>();
            adapter.ownerScene = scene;
            adapter.inGame = scene.path == GamePath;
            adapter.StartCoroutine(adapter.LoadUI());
        }

        private static IEnumerable<T> Components<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
        }

        private IEnumerator LoadUI()
        {
            if (inGame) { pauseUI = this; ConfigureGameplayHUD(); }
            if (!inGame)
            {
                lobbyCanvases.AddRange(Components<Canvas>(ownerScene).Where(canvas => canvas.enabled));
                foreach (var canvas in lobbyCanvases) canvas.enabled = false;
            }
            string path = inGame ? HeatPausePath : HeatMenuPath;
            if (!Application.CanStreamedLevelBeLoaded(path))
            {
                Debug.LogError("Heat UI scene is missing from Build Settings: " + path);
                foreach (var canvas in lobbyCanvases) if (canvas != null) canvas.enabled = true;
                yield break;
            }
            yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(path);
            // The original scene keeps its camera and EventSystem. Heat copies have
            // their demo cameras/EventSystems disabled in serialization, before Awake.
            foreach (var system in Components<EventSystem>(scene)) system.gameObject.SetActive(false);
            yield return null; // Let Heat initialize panel navigation before binding actions.
            if (inGame) BindPause(scene);
            else BindMenu(scene);
        }

        private void ConfigureGameplayHUD()
        {
            var canvas = Components<Canvas>(ownerScene).FirstOrDefault(c => c.name == "GameCanvas");
            if (canvas == null) return;
            foreach (Transform child in canvas.transform)
            {
                if (child.name == "ChatObject")
                {
                    if (ChatSystem.Instance != null) ChatSystem.Instance.ConfigureCompact(child.gameObject);
                    continue;
                }
                if (child.name != "Room stats" && child.name != "Room Details" && child.name != "Inventory Toggel" && child.name != "Inventory UI Panel" && child.name != "PlayerList" && child.name != "Game Pause") continue;
                var group = child.GetComponent<CanvasGroup>() ?? child.gameObject.AddComponent<CanvasGroup>();
                ShowGroup(group, false);
                if (child.name == "PlayerList")
                {
                    roster = group;
                    var rect = child.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(.025f,.64f); rect.anchorMax = new Vector2(.25f,.85f);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
                if (child.name == "Room Details") legacyRoomControls = group;
            }
        }
        private static void ShowGroup(CanvasGroup group, bool show)
        {
            if (group == null) return;
            group.alpha = show ? 1 : 0;
            group.interactable = group.blocksRaycasts = show;
            if (group.gameObject.activeSelf != show) group.gameObject.SetActive(show);
        }

        private void BindMenu(Scene scene)
        {
            heatCanvas = scene.GetRootGameObjects().First(root => root.name == "Canvas - Main Menu");
            foreach (var button in Components<BoxButtonManager>(scene))
            {
                if (button.buttonTitle == "New Game")
                {
                    button.useLocalization = false;
                    button.checkForDoubleClick = false;
                    button.SetText("Jugar cooperativo");
                    button.SetDescription("Elige tu personaje y crea o entra a una sala.");
                    button.onClick = Action(ShowLobby);
                    button.onDoubleClick = new UnityEvent();
                }
                else if (button.buttonTitle == "Continue" || button.buttonTitle == "Load Game")
                {
                    // This game has network sessions, not local save slots.
                    button.gameObject.SetActive(false);
                }
            }
            foreach (var button in Components<PanelButton>(scene))
            {
                if (button.buttonText != "Chapters") continue;
                button.useLocalization = false;
                button.buttonText = "Cooperativo";
                button.UpdateUI();
                button.onClick = Action(ShowLobby);
            }
            CreateBackButton();
            // Connection errors are displayed by the existing popup in Menu.Start.
            if (FusionConnection.Instance != null && FusionConnection.Instance.popup.gameObject.activeSelf)
                ShowLobby();
        }

        private static UnityEvent Action(UnityAction callback)
        {
            var action = new UnityEvent();
            action.AddListener(callback);
            return action;
        }

        private void ShowLobby()
        {
            if (switchingLobby) return;
            switchingLobby = true;
            StartCoroutine(RevealLobby());
        }

        private IEnumerator RevealLobby()
        {
            MissionTransition.Show("Abriendo sala cooperativa...");
            yield return new WaitForSecondsRealtime(.22f);
            heatCanvas.SetActive(false);
            foreach (var canvas in lobbyCanvases) if (canvas != null) canvas.enabled = true;
            backCanvas.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(null);
            MissionTransition.Finish();
            switchingLobby = false;
        }

        private void ShowHome()
        {
            // Return through the same awaited shutdown path, including from the lobby.
            if (FusionConnection.Instance != null) FusionConnection.Instance.ReturnToMenu();
        }

        private void CreateBackButton()
        {
            backCanvas = new GameObject("Return to Heat Menu", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            backCanvas.transform.SetParent(transform, false);
            var canvas = backCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = backCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var buttonObject = new GameObject("Menu principal", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(backCanvas.transform, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-32, -24);
            rect.sizeDelta = new Vector2(240, 52);
            buttonObject.GetComponent<Image>().color = new Color(0.06f, 0.12f, 0.18f, 0.97f);
            buttonObject.GetComponent<Button>().onClick.AddListener(ShowHome);
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(buttonObject.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var text = label.GetComponent<TextMeshProUGUI>();
            text.text = "Menú principal";
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            backCanvas.SetActive(false);
        }

        private void BindPause(Scene scene)
        {
            pauseManager = Components<PauseMenuManager>(scene).Single();
            // One owner for ESC: fields -> tablet/PC -> Heat pause. Start has already run.
            pauseManager.enabled = false;
            // Role selection and terminal canvases use 300 and 250 respectively.
            pauseManager.pauseMenuCanvas.GetComponent<Canvas>().sortingOrder = 400;
            pauseUI = this;
            pauseManager.onOpen.AddListener(OnPauseOpened);
            pauseManager.onClose.AddListener(OnPauseClosed);
            var exit = Components<ModalWindowManager>(scene).Single();
            exit.useLocalization = false;
            exit.titleText = "Salir de la sesión";
            exit.descriptionText = "Volverás al menú. Si la misión sigue activa, se interrumpirá para ambos.";
            exit.onConfirm = Action(ShowHome);
            exit.UpdateUI();
            foreach (var button in Components<PanelButton>(scene))
            {
                if (button.buttonText == "Continue") button.onClick = Action(pauseManager.ClosePauseMenu);
                else if (button.buttonText == "Exit") button.onClick = Action(exit.OpenWindow);
            }
            foreach (var button in Components<ButtonManager>(scene))
                if (button.buttonText == "Continue") button.onClick = Action(pauseManager.ClosePauseMenu);
        }

        private void OnPauseOpened() { pauseOpen = true; RefreshGameplayInput(); }
        private void OnPauseClosed() { pauseOpen = false; RefreshGameplayInput(); }

        private void Update()
        {
            if (!inGame || Keyboard.current == null) return;
            var chat = ChatSystem.Instance;
            if (!IsPaused && !RoleTerminalUI.TabletVisible && !RoleTerminalUI.IsOpen)
            {
                if (Keyboard.current.f2Key.wasPressedThisFrame && chat != null) chat.SetOpen(!chat.IsOpen);
                if (Keyboard.current.f3Key.wasPressedThisFrame && !RoleTerminalUI.IsTyping) rosterOpen = !rosterOpen;
            }
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (chat != null && chat.IsOpen) { chat.SetOpen(false); return; }
            if (rosterOpen) { rosterOpen = false; return; }
            if (IsPaused) ClosePause();
            else if (!RoleTerminalUI.ConsumeEscape()) OpenPause();
        }

        private void LateUpdate()
        {
            if (!inGame) return;
            bool busy = IsPaused || RoleTerminalUI.TabletVisible || RoleTerminalUI.IsOpen;
            if (busy) { rosterOpen = false; if (ChatSystem.Instance != null && ChatSystem.Instance.IsOpen) ChatSystem.Instance.SetOpen(false); }
            ShowGroup(roster, rosterOpen);
            ShowGroup(legacyRoomControls, false);
            RefreshGameplayInput();
        }

        public static void RefreshGameplayInput()
        {
            bool canPlay = RoleManager.Instance != null && RoleManager.Instance.IsOnline &&
                RoleManager.Instance.GameStarted && !IsPaused && !RoleTerminalUI.IsOpen && !RoleTerminalUI.IsTyping &&
                !(ChatSystem.Instance != null && ChatSystem.Instance.IsOpen) && !(pauseUI != null && pauseUI.rosterOpen);
            SetInput(canPlay);
            if (FusionConnection.Instance != null &&
                FusionConnection.Instance.TryGetLocalPlayerComponent(out StarterAssetsInputs input))
                canPlay = !input.IsBlocked;
            Cursor.lockState = canPlay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !canPlay;
        }

        private static void SetInput(bool enabled)
        {
            if (FusionConnection.Instance == null ||
                !FusionConnection.Instance.TryGetLocalPlayerComponent(out StarterAssetsInputs input)) return;
            input.SetGameplayBlocked(!enabled);
        }

        public static bool OpenPause()
        {
            if (pauseUI == null) return false;
            if (pauseUI.pauseManager != null) pauseUI.pauseManager.OpenPauseMenu();
            else pauseUI.OpenFallbackPause();
            return true;
        }

        public static bool ClosePause()
        {
            if (pauseUI == null) return false;
            if (pauseUI.pauseManager != null) pauseUI.pauseManager.ClosePauseMenu();
            else { if (pauseUI.fallbackPause != null) pauseUI.fallbackPause.SetActive(false); pauseUI.OnPauseClosed(); }
            return true;
        }

        private void OpenFallbackPause()
        {
            if (fallbackPause == null)
            {
                var skin = IncidentUISkin.CreateDefault();
                var canvas = UIFactory.CreateCanvas("Session menu", transform, 400, new Vector2(1920,1080));
                fallbackPause = canvas.gameObject;
                var card = UIFactory.CreatePanel("Menu", canvas.transform, new Vector2(.35f,.32f), new Vector2(.65f,.68f), new Color(.025f,.045f,.065f,.98f), 0, skin);
                UIFactory.CreateText("Title",card.transform,"CYBER DEFENSE",28,new Vector2(.08f,.73f),new Vector2(.92f,.92f),TextAlignmentOptions.Center,Color.white,skin);
                UIFactory.CreateButton("Continue",card.transform,"Continuar",new Vector2(.1f,.42f),new Vector2(.9f,.64f),skin.buttonPrimary,skin).onClick.AddListener(()=>ClosePause());
                UIFactory.CreateButton("Exit",card.transform,"Volver al menu",new Vector2(.1f,.12f),new Vector2(.9f,.34f),skin.buttonPrimary,skin).onClick.AddListener(ShowHome);
            }
            fallbackPause.SetActive(true); OnPauseOpened();
        }

        private void OnDestroy()
        {
            if (pauseUI == this) pauseUI = null;
            if (pauseManager != null)
            {
                pauseManager.onOpen.RemoveListener(OnPauseOpened);
                pauseManager.onClose.RemoveListener(OnPauseClosed);
            }
        }
    }
}
