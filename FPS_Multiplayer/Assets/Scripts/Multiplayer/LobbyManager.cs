//using System.Collections.Generic;
//using Fusion;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class LobbyManager : MonoBehaviour
//{
//    public GameObject lobbyPanel;
//    public GameObject characterSelectionPanel;
//    public NetworkRunnerManager spawner;

//    [Header("Character Selection")]
//    public TMP_InputField playerNameInput;

//    [Header("Room List")]
//    public GameObject roomListParent;
//    public GameObject roomListItemPrefab;
//    public TMP_InputField roomNameInput;

//    async void Start()
//    {
//        // Ẩn bảng chọn phòng, hiện bảng nhập tên lúc đầu
//        lobbyPanel.SetActive(false);
//        characterSelectionPanel.SetActive(true);

//        if (spawner == null) spawner = FindFirstObjectByType<NetworkRunnerManager>();

//        // Kết nối vào Lobby mạng
//        await spawner.StartLobby();
//    }

//    public void OnNextButton()
//    {
//        var playerName = playerNameInput.text;
//        if (string.IsNullOrEmpty(playerName))
//        {
//            Debug.LogWarning("Player name cannot be empty!");
//            return;
//        }

//        // Tạm thời bỏ qua PlayerProfile để tránh lỗi đỏ
//        Debug.Log($"Player Name: {playerName}");

//        // Chuyển sang bảng danh sách phòng
//        characterSelectionPanel.SetActive(false);
//        lobbyPanel.SetActive(true);
//    }

//    // Hiển thị danh sách phòng (Tự đẻ ra các nút RoomItem xanh dương)
//    public void DisplayRoomList(List<SessionInfo> sessions)
//    {
//        foreach (Transform child in roomListParent.transform)
//        {
//            Destroy(child.gameObject);
//        }

//        if (sessions.Count == 0) return;

//        foreach (var session in sessions)
//        {
//            var item = Instantiate(roomListItemPrefab, roomListParent.transform);
//            var text = item.GetComponentInChildren<TextMeshProUGUI>();
//            text.text = $"{session.Name} ({session.PlayerCount}/{session.MaxPlayers})";

//            var button = item.GetComponent<Button>();
//            button.onClick.AddListener(() => OnJoinRoom(session.Name));
//            item.SetActive(true);
//        }
//    }

//    async void OnJoinRoom(string sessionName)
//    {
//        await spawner.JoinGame(sessionName);
//    }

//    public async void OnCreateRoomButton()
//    {
//        var roomName = roomNameInput.text;
//        if (string.IsNullOrEmpty(roomName))
//        {
//            Debug.LogWarning("Room name cannot be empty!");
//            return;
//        }

//        // Tạo phòng và nhảy vào Scene 1 (Map FPS 3D)
//        await spawner.StartHost(roomName, 20, SceneRef.FromIndex(2));
//    }
//}

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

    // Hiển thị danh sách phòng (Đã chia làm 2 cột Name | Players cho đẹp)
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

            // Tìm 2 cái Text trong Prefab (cái mà ông đã chia đất 70/30)
            var txts = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (txts.Length >= 2)
            {
                txts[0].text = session.Name; // Cột tên
                txts[1].text = $"{session.PlayerCount}/{session.MaxPlayers}"; // Cột số người
            }

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
        if (string.IsNullOrEmpty(roomName)) return;
        await spawner.StartHost(roomName, 20, SceneRef.FromIndex(2));
    }
}
