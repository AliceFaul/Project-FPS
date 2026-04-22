using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class ChatUI : MonoBehaviour {
    [Header("Property references")]
    [SerializeField] private Transform chatContent;
    [SerializeField] private GameObject chatItem;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private int maxMessages = 15;

    [Header("Fade Setting")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float fadeDelay = 4f;
    [SerializeField] private float fadeDuration = 1.5f;

    private Queue<GameObject> _messages = new Queue<GameObject>();
    
    private bool _isFading = false;
    private float _lastMessageTime;
    private Coroutine _focusCoroutine;
    private Coroutine _fadeCoroutine;
    private bool _isSubscribed;
    private bool _isChatOpen;
#if ENABLE_INPUT_SYSTEM
    private PlayerInput _localPlayerInput;
#endif

    private void OnEnable() {
        TrySubscribe();
    }

    private void OnDisable() {
        if(_isSubscribed && ChatManager.Instance != null) { 
            ChatManager.Instance.OnMessageReceived -= HandleMessage;
        }
        _isSubscribed = false;
    }

    private void Start() {
        if(chatPanel != null) {
            chatPanel.gameObject.SetActive(false);
        }
        if(panelCanvasGroup != null) {
            panelCanvasGroup.alpha = 0f;
        }
        Cursor.lockState = CursorLockMode.Locked;
        ApplyGameplayInputState(false);
    }

    private void Update() {
        if(!_isSubscribed) {
            TrySubscribe();
        }

        if(WasEnterPressed()) { 
            if(chatInput.IsActive()) {
                Submit();
            } else {
                Open();
            }
        }
        HandleFade();
    }

    private void Open() {
        _isChatOpen = true;
        chatPanel.gameObject.SetActive(true);
        if(panelCanvasGroup != null) {
            panelCanvasGroup.alpha = 1f;
        }
        StopFade();
        Cursor.lockState = CursorLockMode.None;

        if(_focusCoroutine != null) {
            StopCoroutine(_focusCoroutine);
        }

        _focusCoroutine = StartCoroutine(FocusInputNextFrame());
    }

    private void Close() {
        _isChatOpen = false;
        _lastMessageTime = Time.time;
        chatInput.text = "";
        chatInput.DeactivateInputField();
        if(EventSystem.current != null) {
            EventSystem.current.SetSelectedGameObject(null);
        }
        Cursor.lockState = CursorLockMode.Locked;
        ApplyGameplayInputState(false);
    }

    public void ToggleChatButton() {
        chatPanel.gameObject.SetActive(!chatPanel.activeSelf);
    }

    private void Submit() { 
        if(string.IsNullOrWhiteSpace(chatInput.text)) {
            return;
        }

        string message = chatInput.text;
        if(ChatManager.Instance != null) {
            ChatManager.Instance.SendChat(message);
        }

        Close();
    }

    private void HandleMessage(string message) { 
        chatPanel.gameObject.SetActive(true);
        AddMessage(message);
        _lastMessageTime = Time.time;
        if(panelCanvasGroup != null) {
            panelCanvasGroup.alpha = 1f;
        }
        StopFade();
    }

    private void AddMessage(string message) {
        var obj = Instantiate(chatItem, chatContent);
        var text = obj.GetComponent<TMP_Text>();
        if(text == null) {
            text = obj.GetComponentInChildren<TMP_Text>(true);
        }
        if(text != null) {
            text.text = message;
        }
        _messages.Enqueue(obj);
        if(_messages.Count > maxMessages) {
            Destroy(_messages.Dequeue());
        }
    }

    private void HandleFade() { 
        if(panelCanvasGroup == null) {
            return;
        }
        if(_isChatOpen) {
            panelCanvasGroup.alpha = 1f;
            StopFade();
            return;
        }
        if(IsChatEditing()) {
            panelCanvasGroup.alpha = 1f;
            return;
        }
        if(Time.time - _lastMessageTime < fadeDelay) {
            return;
        }
        if(!_isFading) {
            StopFade();
            _fadeCoroutine = StartCoroutine(FadeOut());
        }
    }

    private IEnumerator FadeOut() {
        _isFading = true;
        float start = panelCanvasGroup.alpha;
        float t = 0f;
        while(t < fadeDuration) {
            t += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 0f;
        if(!IsChatEditing()) {
            chatPanel.gameObject.SetActive(false);
        }
        _isFading = false;
        _fadeCoroutine = null;
    }

    private void TrySubscribe() {
        if(_isSubscribed || ChatManager.Instance == null) {
            return;
        }

        ChatManager.Instance.OnMessageReceived += HandleMessage;
        _isSubscribed = true;
    }

    private void StopFade() {
        if(_fadeCoroutine != null) {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
        _isFading = false;
        if(_isChatOpen && panelCanvasGroup != null) {
            panelCanvasGroup.alpha = 1f;
        }
    }

    private bool WasEnterPressed() {
        bool legacyEnterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

#if ENABLE_INPUT_SYSTEM
        if(Keyboard.current != null) {
            return legacyEnterPressed
                || Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
        }
#endif

        return legacyEnterPressed;
    }

    private bool IsChatEditing() {
        if(chatInput == null) {
            return false;
        }
        if(!_isChatOpen || !chatPanel.gameObject.activeInHierarchy) {
            return false;
        }
        bool isFocused = chatInput.isFocused;
        if(!isFocused && EventSystem.current != null) {
            isFocused = EventSystem.current.currentSelectedGameObject == chatInput.gameObject;
        }
        ApplyGameplayInputState(isFocused);
        if(isFocused) {
            return true;
        }
        return false;
    }

    private IEnumerator FocusInputNextFrame() {
        yield return null;

        if(chatInput == null || !chatPanel.gameObject.activeInHierarchy) {
            _focusCoroutine = null;
            yield break;
        }

        if(EventSystem.current != null) {
            EventSystem.current.SetSelectedGameObject(chatInput.gameObject);
        }

        chatInput.Select();
        chatInput.ActivateInputField();
        chatInput.caretPosition = chatInput.text.Length;
        ApplyGameplayInputState(true);
        _focusCoroutine = null;
    }

    private void ApplyGameplayInputState(bool isChatEditing) {
#if ENABLE_INPUT_SYSTEM
        if(_localPlayerInput == null) {
            _localPlayerInput = FindLocalPlayerInput();
        }

        if(_localPlayerInput != null) {
            _localPlayerInput.enabled = !isChatEditing;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static PlayerInput FindLocalPlayerInput() {
        PlayerInput[] playerInputs = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        foreach(PlayerInput playerInput in playerInputs) {
            if(playerInput != null && playerInput.enabled) {
                return playerInput;
            }
        }

        return null;
    }
#endif
}
