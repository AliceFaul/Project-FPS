using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class NetworkRunnerManager : MonoBehaviour, INetworkRunnerCallbacks {
    private NetworkRunner _runner;

    // Dictionary to keep track of spawned characters for each player
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private Dictionary<PlayerRef, Vector3> _spawnPositions = new Dictionary<PlayerRef, Vector3>();
    private Dictionary<PlayerRef, Quaternion> _spawnRotations = new Dictionary<PlayerRef, Quaternion>();
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnRadius = 3f;

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
            _spawnPositions.Clear();
            _spawnRotations.Clear();
        }

        SceneManager.LoadScene(currentScene);
    }
    
    // function spawn character for player when player joined room 
    // and despawn character when player left room
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { 
        if(_runner.IsServer) {
            ResolveSpawnPose(player, runner, out Vector3 position, out Quaternion rotation);
            _spawnPositions[player] = position;
            _spawnRotations[player] = rotation;
            NetworkObject character = _runner.Spawn(
                playerPrefab,
                position,
                rotation,
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
        _spawnRotations.Remove(player);
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

        Quaternion spawnRotation;
        if(!TryGetSpawnPosition(networkObject.InputAuthority, out Vector3 spawnPosition)) {
            ResolveSpawnPose(networkObject.InputAuthority, _runner, out spawnPosition, out spawnRotation);
        } else if(!_spawnRotations.TryGetValue(networkObject.InputAuthority, out spawnRotation)) {
            spawnRotation = Quaternion.identity;
        }

        Transform playerTransform = playerHealth.transform;
        CharacterController characterController = playerHealth.GetComponent<CharacterController>();
        if(characterController != null) {
            characterController.enabled = false;
        }

        playerTransform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if(characterController != null) {
            characterController.enabled = true;
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) {
        StarterAssetsInputs starterAssetsInputs = FindLocalStarterAssetsInputs();
        if(starterAssetsInputs == null) {
            return;
        }

        NetworkButtons buttons = default;
        if(starterAssetsInputs.jump) {
            buttons.Set((int)NetworkPlayerButtons.Jump, true);
        }
        if(starterAssetsInputs.sprint) {
            buttons.Set((int)NetworkPlayerButtons.Sprint, true);
        }

        input.Set(new NetworkPlayerInputData {
            Move = starterAssetsInputs.move,
            Look = starterAssetsInputs.look,
            Buttons = buttons,
        });
    }
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
    public void OnSceneLoadDone(NetworkRunner runner) {
        spawnPoint = FindSpawnPointInLoadedScene();

        if(runner.IsServer) {
            UpdateSpawnAssignmentsForExistingPlayers(runner);
        }
    }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    private static StarterAssetsInputs FindLocalStarterAssetsInputs() {
        StarterAssetsInputs[] inputs = FindObjectsByType<StarterAssetsInputs>(FindObjectsSortMode.None);
        foreach(StarterAssetsInputs candidate in inputs) {
            if(candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy) {
                continue;
            }

#if ENABLE_INPUT_SYSTEM
            PlayerInput playerInput = candidate.GetComponent<PlayerInput>();
            if(playerInput != null && !playerInput.enabled) {
                continue;
            }
#endif

            return candidate;
        }

        return null;
    }

    private void ResolveSpawnPose(PlayerRef player, NetworkRunner runner, out Vector3 position, out Quaternion rotation) {
        Transform resolvedSpawnPoint = ResolveSpawnPoint();
        if(resolvedSpawnPoint != null) {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 center = resolvedSpawnPoint.position;
            position = new Vector3(
                center.x + randomOffset.x,
                center.y,
                center.z + randomOffset.y
            );
            rotation = resolvedSpawnPoint.rotation;
            Debug.Log($"Assigned spawn position near {resolvedSpawnPoint.name} to player {player}");
            return;
        }

        position = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);
        rotation = Quaternion.identity;
        Debug.LogWarning($"No spawn point found in scene. Using fallback spawn position {position} for player {player}");
    }

    private Transform ResolveSpawnPoint() {
        Transform loadedSceneSpawnPoint = FindSpawnPointInLoadedScene();
        if(loadedSceneSpawnPoint != null) {
            spawnPoint = loadedSceneSpawnPoint;
            return spawnPoint;
        }

        return null;
    }

    private static Transform FindSpawnPointInLoadedScene() {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach(Transform sceneTransform in transforms) {
            if(sceneTransform == null || !sceneTransform.gameObject.scene.isLoaded) {
                continue;
            }

            if(string.Equals(sceneTransform.name, "SpawnPoint", StringComparison.OrdinalIgnoreCase)) {
                return sceneTransform;
            }
        }

        return null;
    }

    // function to update spawn position and rotation for existing players 
    // when host load scene
    private void UpdateSpawnAssignmentsForExistingPlayers(NetworkRunner runner) {
        foreach(KeyValuePair<PlayerRef, NetworkObject> entry in _spawnedCharacters) {
            if(entry.Value == null) {
                continue;
            }
            // Recalculate spawn position and rotation for the player 
            // based on the new scene's spawn points
            ResolveSpawnPose(entry.Key, runner, out Vector3 position, out Quaternion rotation);
            _spawnPositions[entry.Key] = position;
            _spawnRotations[entry.Key] = rotation;

            Transform characterTransform = entry.Value.transform;
            characterTransform.SetPositionAndRotation(position, rotation);
        }
    }
}
