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
            heatCanvas.SetActive(false);
            foreach (var canvas in lobbyCanvases) if (canvas != null) canvas.enabled = true;
            backCanvas.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(null);
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
            // Role selection and terminal canvases use 300 and 250 respectively.
            pauseManager.pauseMenuCanvas.GetComponent<Canvas>().sortingOrder = 400;
            pauseUI = this;
            pauseManager.onOpen.AddListener(OnPauseOpened);
            pauseManager.onClose.AddListener(OnPauseClosed);
            var exit = Components<ModalWindowManager>(scene).Single();
            exit.useLocalization = false;
            exit.titleText = "Salir de la sesión";
            exit.descriptionText = "Volverás al menú principal. El otro jugador puede seguir conectado.";
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

        private void LateUpdate()
        {
            if (!inGame) return;
            RefreshGameplayInput();
        }

        public static void RefreshGameplayInput()
        {
            bool canPlay = RoleManager.Instance != null && RoleManager.Instance.IsOnline &&
                RoleManager.Instance.GameStarted && !IsPaused && !RoleTerminalUI.IsOpen;
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
            if (pauseUI == null || pauseUI.pauseManager == null) return false;
            pauseUI.pauseManager.OpenPauseMenu();
            return true;
        }

        public static bool ClosePause()
        {
            if (pauseUI == null || pauseUI.pauseManager == null) return false;
            pauseUI.pauseManager.ClosePauseMenu();
            return true;
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
