#if CMPSETUP_COMPLETE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ChatSystem : MonoBehaviour
{
    [SerializeField] private ChatItem chatPrefab;
    [SerializeField] private GameObject  chatContainer;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private TextMeshProUGUI chatLimitDisplay;
    [SerializeField] private ScrollRect scrollRect;
    public static ChatSystem Instance = null;
    private ChatPlayer _chatPlayer;

    private bool _hasUserScroll;
    private CanvasGroup compactGroup;
    public bool IsOpen { get; private set; }
    public int Unread { get; private set; }

    public void ConfigureCompact(GameObject root)
    {
        compactGroup = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.025f,.075f); rect.anchorMax = new Vector2(.29f,.33f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        if (chatInput.placeholder is TMP_Text placeholder) placeholder.text = "Mensaje al equipo...";
        SetOpen(false);
    }

    public void SetOpen(bool value)
    {
        if (compactGroup == null) return;
        IsOpen = value;
        compactGroup.alpha = value ? 1 : 0;
        compactGroup.interactable = compactGroup.blocksRaycasts = value;
        if (compactGroup.gameObject.activeSelf != value) compactGroup.gameObject.SetActive(value);
        if (value) { Unread = 0; chatInput.ActivateInputField(); }
        else
        {
            chatInput.DeactivateInputField();
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == chatInput.gameObject)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            if (_chatPlayer != null) _chatPlayer.SetPlayerIsWriting(false);
        }
    }

    private void Awake()
    {
        Instance = this;
        chatInput.onFocusSelectAll = false;
        scrollRect.onValueChanged.AddListener((x) =>
        {
            _hasUserScroll = true;
        });
    }

    private void OnEnable()
    {
        chatInput.onSelect.AddListener(InputInFocus);
        chatInput.onDeselect.AddListener(InputLostFocus);
        chatInput.onSubmit.AddListener(InputSubmit);
        chatInput.onValueChanged.AddListener(CharacterCountUpdate);
    }

    private void OnDisable()
    {
        if (_chatPlayer != null) _chatPlayer.SetPlayerIsWriting(false);
        chatInput.onSelect.RemoveListener(InputInFocus);
        chatInput.onDeselect.RemoveListener(InputLostFocus);
        chatInput.onSubmit.RemoveListener(InputSubmit);
        chatInput.onValueChanged.RemoveListener(CharacterCountUpdate);
    }

    public void SetChatPlayer(ChatPlayer chatPlayer)
    {
        _chatPlayer = chatPlayer;
        _chatPlayer.SetPlayerIsWriting(chatInput.isFocused);
    }

    public void AddChatEntry(bool isLeft,Chat chat)
    {
        if (isLeft && !IsOpen) Unread++;
        var go = Instantiate(chatPrefab, chatContainer.transform);
        go.Init(isLeft,chat);
        if (_hasUserScroll && !(scrollRect.verticalNormalizedPosition <= 0.1f))
            return;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void InputInFocus(string text)
    {
        chatInput.MoveTextEnd(false);
        if (_chatPlayer != null) _chatPlayer.SetPlayerIsWriting(true);
    }

    public void InputLostFocus(string text)
    {
        if (_chatPlayer != null) _chatPlayer.SetPlayerIsWriting(false);
    }


    public void InputSubmit(string text)
    {
        if (_chatPlayer != null && text != "")
        {
            chatInput.ActivateInputField();
            var trimmedText = text.Trim();
            var newChat = new Chat(_chatPlayer.playerName, trimmedText);
            chatInput.text = "";
            _chatPlayer.SendChat(newChat);
        }
    }

    public void CharacterCountUpdate(string text)
    {
        chatLimitDisplay.text = text.Count() == chatInput.characterLimit
            ? $"<color=#D96222>{text.Count()}/{chatInput.characterLimit}</color>"
            : $"{text.Count()}/{chatInput.characterLimit}";
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
#endif
