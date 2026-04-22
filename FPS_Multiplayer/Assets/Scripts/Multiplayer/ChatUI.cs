using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

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

    private void OnEnable() {
        if(ChatManager.Instance != null) {
            ChatManager.Instance.OnMessageReceived += AddMessage;
        }
    }

    private void OnDisable() {
        if(ChatManager.Instance != null) { 
            ChatManager.Instance.OnMessageReceived -= AddMessage;
        }
    }

    private void Start() {
        chatPanel.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) { 
            if(chatInput.IsActive()) {
                Submit();
                Close();
            } else {
                Open();
            }
        }
    }

    private void Open() {
        chatPanel.gameObject.SetActive(true);
        chatInput.ActivateInputField();
        Cursor.lockState = CursorLockMode.None;
    }

    private void Close() {
        chatInput.text = "";
        chatInput.DeactivateInputField();
        chatPanel.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void ToggleChatButton() {
        chatPanel.gameObject.SetActive(!chatPanel.activeSelf);
    }

    private void Submit() { 
        if(string.IsNullOrWhiteSpace(chatInput.text)) {
            return;
        }
        if(ChatManager.Instance != null) {
            ChatManager.Instance.SendChat(chatInput.text);
        }
    }

    private void AddMessage(string message) {
        var obj = Instantiate(chatItem, chatContent);
        var text = obj.GetComponent<TMP_Text>();
        text.text = message;
        _messages.Enqueue(obj);
        if(_messages.Count > maxMessages) {
            Destroy(_messages.Dequeue());
        }
        Canvas.ForceUpdateCanvases();
    }

    private void HandleFade() { 

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
        _isFading = false;
    }
}
