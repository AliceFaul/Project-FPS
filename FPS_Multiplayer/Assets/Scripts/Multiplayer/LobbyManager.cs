using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public GameObject lobbyPanel;
    public GameObject characterSelectionPanel;
    public NetworkRunnerManager spawner;

    [Header("Character Selection")]
    public TMP_InputField playerNameInput;
    public GameObject confirmMessage; // Thêm dòng này: Kéo cái Text thông báo vào đây

    [Header("Room List")]
    public GameObject roomListParent;
    public GameObject roomListItemPrefab;
    public TMP_InputField roomNameInput;

    async void Start()
    {
        // Ẩn thông báo xác nhận lúc đầu
        if (confirmMessage != null) confirmMessage.SetActive(false);

        // Giữ nguyên logic ban đầu của ông
        lobbyPanel.SetActive(false);
        characterSelectionPanel.SetActive(true);

        if (spawner == null) spawner = FindFirstObjectByType<NetworkRunnerManager>();
        if(playerNameInput != null) {
            playerNameInput.text = PlayerNameStorage.GetPlayerName();
        }
        await spawner.StartLobby();
    }

    public void OnNextButton() // Đây chính là hàm chạy khi bấm Confirm
    {
        if(playerNameInput == null)
        {
            return;
        }

        string playerName = PlayerNameStorage.Sanitize(playerNameInput.text);
        if(string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Player name cannot be empty!");
            return;
        }

        PlayerNameStorage.SavePlayerName(playerName);
        playerNameInput.text = playerName;

        Debug.Log($"Player Name saved: {playerName}");

        // --- PHẦN THÊM MỚI ---
        if (confirmMessage != null)
        {
            confirmMessage.SetActive(true); // Hiện thông báo "Đã xác nhận tên"
        }
        // ---------------------

        if(characterSelectionPanel != null) {
            characterSelectionPanel.SetActive(false);
        }
        if(lobbyPanel != null) {
            lobbyPanel.SetActive(true);
        }
    }

    // Hiển thị danh sách phòng
    public void DisplayRoomList(List<SessionInfo> sessions) {
        foreach (Transform child in roomListParent.transform) {
            Destroy(child.gameObject);
        }

        foreach (var session in sessions) {
            var roomItem = Instantiate(roomListItemPrefab, roomListParent.transform);
            var roomName = roomItem.GetComponentInChildren<TMP_Text>();
            if(roomName != null) {
                roomName.text = $"{session.Name} ({session.PlayerCount}/{session.MaxPlayers})";
            }
            var joinButton = roomItem.GetComponentInChildren<Button>();
            if(joinButton != null) {
                joinButton.onClick.AddListener(() => OnJoinRoom(session.Name));
            }
            roomItem.SetActive(true);
        }
    }

    async void OnJoinRoom(string sessionName)
    {
        await spawner.JoinGame(sessionName);
    }

    public async void OnCreateRoomButton()
    {
        var roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName)) return;
        await spawner.StartHost(roomName, 5, SceneRef.FromIndex(2));
    }
}
