using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class NetworkRunnerManager : MonoBehaviour, INetworkRunnerCallbacks {
    private NetworkRunner _runner;

    private void Awake() {
        if(_runner == null) {
            _runner = gameObject.AddComponent<NetworkRunner>();
        }
        DontDestroyOnLoad(this);
    }

    // function let player join lobby in start of game
    public async Task StartLobby() {
        if(_runner == null) {
            _runner = gameObject.AddComponent<NetworkRunner>();
        }
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);
        var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer, "default-lobby");
        if(result.Ok) {
            Debug.Log("Join lobby completed");
        } else {
            Debug.Log($"Failed to join lobby: {result.ShutdownReason}");
        }
    }

    // function let host create room and started as host
    public async Task StartHost(string sessionName, int playerCount, SceneRef sceneRef) {
        if(_runner == null) {
            return;
        }
        var result = await _runner.StartGame(new StartGameArgs {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            Scene = sceneRef,
            PlayerCount = playerCount,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
        // Check state of result and debug on console
        if(result.Ok) {
            Debug.Log("Started host completed");
        } else {
            Debug.Log($"Failed to start host: {result.ShutdownReason}");
        }
    }

    // function let player join room in room list as client mode
    public async Task JoinGame(string sessionName) {
        if(_runner == null) {
            return;
        }
        var result = await _runner.StartGame(new StartGameArgs {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
        if(result.Ok) {
            Debug.Log("Join game as client completed");
        } else {
            Debug.Log($"Failed to join game: {result.ShutdownReason}");
        }
    }
    
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
