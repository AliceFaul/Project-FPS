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

    [Header("Room List")]
    public GameObject roomListParent;
    public GameObject roomListItemPrefab;
    public TMP_InputField roomNameInput;

    async void Start()
    {
        // Ẩn bảng chọn phòng, hiện bảng nhập tên lúc đầu
        lobbyPanel.SetActive(false);
        characterSelectionPanel.SetActive(true);

        if (spawner == null) spawner = FindFirstObjectByType<NetworkRunnerManager>();

        // Kết nối vào Lobby mạng
        await spawner.StartLobby();
    }

    public void OnNextButton()
    {
        var playerName = playerNameInput.text;
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Player name cannot be empty!");
            return;
        }

        // Tạm thời bỏ qua PlayerProfile để tránh lỗi đỏ
        Debug.Log($"Player Name: {playerName}");

        // Chuyển sang bảng danh sách phòng
        characterSelectionPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    // Hiển thị danh sách phòng (Tự đẻ ra các nút RoomItem xanh dương)
    public void DisplayRoomList(List<SessionInfo> sessions)
    {
        foreach (Transform child in roomListParent.transform)
        {
            Destroy(child.gameObject);
        }

        if (sessions.Count == 0) return;

        foreach (var session in sessions)
        {
            var item = Instantiate(roomListItemPrefab, roomListParent.transform);
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            text.text = $"{session.Name} ({session.PlayerCount}/{session.MaxPlayers})";

            var button = item.GetComponent<Button>();
            button.onClick.AddListener(() => OnJoinRoom(session.Name));
            item.SetActive(true);
        }
    }

    async void OnJoinRoom(string sessionName)
    {
        await spawner.JoinGame(sessionName);
    }

    public async void OnCreateRoomButton()
    {
        var roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("Room name cannot be empty!");
            return;
        }

        // Tạo phòng và nhảy vào Scene số 1 (Map FPS 3D của ông bạn)
        await spawner.StartHost(roomName, 20, SceneRef.FromIndex(2));
    }
}