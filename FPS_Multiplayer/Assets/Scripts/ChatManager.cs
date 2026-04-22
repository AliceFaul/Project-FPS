using Fusion;
using TMPro;
using UnityEngine;

public class ChatManager : NetworkBehaviour
{
    public TMP_InputField chatInput;
    public TextMeshProUGUI chatContent;
    public GameObject chatPanel;

    public override void Spawned()
    {
        // Khi mới vào game, ẩn ô nhập đi cho giống Roblox
        if (Object.HasInputAuthority)
        {
            chatInput.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void Update()
    {
        if (!Object.HasInputAuthority) return;

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
        Cursor.lockState = CursorLockMode.None; // Hiện chuột để bấm
    }

    void CloseChat()
    {
        chatInput.text = "";
        chatInput.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked; // Khóa chuột để bắn súng
    }

    public void SendChat()
    {
        // Lấy tên từ PlayerData (script Singleton mình làm ở Lobby)
        string playerName = PlayerData.Instance != null ? PlayerData.Instance.PlayerName : "Player";
        string message = $"<b>{playerName}:</b> {chatInput.text}";

        RPC_RelayMessage(message);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_RelayMessage(string finalMessage)
    {
        if (chatContent != null)
        {
            chatContent.text += "\n" + finalMessage;
            Canvas.ForceUpdateCanvases();
            // Lệnh này giúp Text luôn tự cuộn xuống dòng mới nhất
        }
    }
}