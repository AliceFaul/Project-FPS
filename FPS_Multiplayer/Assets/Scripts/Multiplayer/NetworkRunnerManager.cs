using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkRunnerManager : MonoBehaviour, INetworkRunnerCallbacks {
    private NetworkRunner _runner;

    // Dictionary to keep track of spawned characters for each player
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private Dictionary<PlayerRef, Vector3> _spawnPositions = new Dictionary<PlayerRef, Vector3>();
    [SerializeField] private NetworkPrefabRef playerPrefab;

    private void Awake() {
        if(_runner == null) {
            _runner = gameObject.AddComponent<NetworkRunner>();
        }
        DontDestroyOnLoad(this);
    }

    private async void Start() {
        await StartLobby();
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

    public void OnStartHost() {
        StartGame(GameMode.Host);
    }

    public void OnStartClient() {
        StartGame(GameMode.Client);
    }

    private async void StartGame(GameMode mode) {
        if(_runner == null) {
            return;
        }
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex + 1);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid) {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }
        var result = await _runner.StartGame(new StartGameArgs {
            GameMode = mode,
            SessionName = "TestSession",
            Scene = scene,
            PlayerCount = 10,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
        if(result.Ok) {
            Debug.Log("Start game completed");
        } else {
            Debug.Log($"Failed to start game: {result.ShutdownReason}");
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

    public async void RestartCurrentScene() {
        int currentScene = SceneManager.GetActiveScene().buildIndex;

        if(_runner != null && _runner.IsRunning) {
            await _runner.Shutdown();
            _spawnedCharacters.Clear();
        }

        SceneManager.LoadScene(currentScene);
    }
    
    // function spawn character for player when player joined room 
    // and despawn character when player left room
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { 
        if(_runner.IsServer) {
            Vector3 position = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);
            _spawnPositions[player] = position;
            NetworkObject character = _runner.Spawn(
                playerPrefab,
                position,
                Quaternion.identity,
                player
            );
            _spawnedCharacters[player] = character;
        }
    }

    // function let player leave room and despawn character when player left
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { 
        if(_spawnedCharacters.TryGetValue(player, out NetworkObject character)) {
            _runner.Despawn(character);
            _spawnedCharacters.Remove(player);
        }
        _spawnPositions.Remove(player);
    }

    public bool TryGetSpawnPosition(PlayerRef player, out Vector3 spawnPosition) {
        return _spawnPositions.TryGetValue(player, out spawnPosition);
    }

    public void RespawnPlayer(PlayerHealth playerHealth) {
        if(playerHealth == null) {
            return;
        }

        NetworkObject networkObject = playerHealth.GetComponent<NetworkObject>();
        if(networkObject == null) {
            return;
        }

        if(!TryGetSpawnPosition(networkObject.InputAuthority, out Vector3 spawnPosition)) {
            spawnPosition = new Vector3((networkObject.InputAuthority.RawEncoded % _runner.Config.Simulation.PlayerCount) * 3, 1, 0);
        }

        Transform playerTransform = playerHealth.transform;
        CharacterController characterController = playerHealth.GetComponent<CharacterController>();
        if(characterController != null) {
            characterController.enabled = false;
        }

        playerTransform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

        if(characterController != null) {
            characterController.enabled = true;
        }
    }

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
