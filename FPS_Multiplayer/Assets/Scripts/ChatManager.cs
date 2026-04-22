using Fusion;
using TMPro;
using UnityEngine;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Instance;

    [Header("UI References")]
    public TMP_InputField chatInput;
    public GameObject chatPanel;
    public Transform chatContentParent; // Kéo cái 'Content' của Scroll View vào đây

    [Header("Prefabs")]
    public GameObject chatItemPrefab; // Kéo cái 'ChatItem' prefab của bạn ông vào đây

    private void Awake()
    {
        // Singleton để có thể gọi từ các script khác nếu cần
        if (Instance == null) Instance = this;
    }

    public override void Spawned()
    {
        // Khi vào game, đảm bảo ô chat đóng và khóa chuột để bắn súng
        // Vì script nằm trên Object rỗng trong Scene, ta dùng HasStateAuthority hoặc kiểm tra chung
        chatInput.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Nhấn Enter để mở/đóng ô chat
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (chatInput.gameObject.activeSelf)
            {
                if (!string.IsNullOrEmpty(chatInput.text))
                {
                    SendChat();
                }
                CloseChat();
            }
            else
            {
                OpenChat();
            }
        }
    }

    void OpenChat()
    {
        chatInput.gameObject.SetActive(true);
        chatInput.ActivateInputField();
        Cursor.lockState = CursorLockMode.None; // Hiện chuột để gõ
    }

    void CloseChat()
    {
        chatInput.text = "";
        chatInput.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked; // Khóa chuột để quay lại chiến đấu
    }

    public void SendChat()
    {
        // Lấy tên từ bộ nhớ (đã xác nhận ở Lobby)
        string playerName = PlayerNameStorage.GetPlayerName();
        string fullMessage = $"<b>{playerName}:</b> {chatInput.text}";

        // Gửi RPC từ người chơi đến tất cả mọi người
        RPC_RelayMessage(fullMessage);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_RelayMessage(string finalMessage)
    {
        // Đẻ ra một dòng chat mới từ Prefab của bạn ông làm
        GameObject newMessage = Instantiate(chatItemPrefab, chatContentParent);

        // Gán nội dung vào Text bên trong Prefab đó
        var textComponent = newMessage.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = finalMessage;
        }

        // Tự động cuộn xuống dưới cùng
        Canvas.ForceUpdateCanvases();
    }
}