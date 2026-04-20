using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

// Ghi nhận nút bấm cho FPS 3D
public struct PlayerInputData : INetworkInput
{
    public Vector2 Direction; // WASD
}

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner { get; set; }
    public LobbyManager LobbyManager;
    [SerializeField] private NetworkPrefabRef _playerPrefab; // Kéo Prefab nhân vật FPS vào đây
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        DontDestroyOnLoad(gameObject); // Giữ bộ não này không bị xóa khi chuyển cảnh
    }

    public async Task StartLobby()
    {
        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);
        await _runner.JoinSessionLobby(SessionLobby.ClientServer);
    }

    public async Task StartHost(string sessionName, SceneRef scene)
    {
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public async Task StartClient(string sessionName)
    {
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    // XỬ LÝ KHI VÀO GAME (FPS 3D)
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // ĐỔI THÀNH VECTOR3 CHO GAME 3D
            Vector3 spawnPosition = new Vector3(Random.Range(-5, 5), 1, Random.Range(-5, 5));
            var networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new PlayerInputData();
        data.Direction = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        input.Set(data);
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (LobbyManager != null) LobbyManager.DisplayRoomList(sessionList);
    }

    // --- CÁC HÀM PHỤ ĐỂ TRỐNG ĐỂ TRÁNH LỖI ---
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject obj))
        {
            runner.Despawn(obj);
            _spawnedCharacters.Remove(player);
        }
    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}