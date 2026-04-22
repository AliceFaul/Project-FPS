using Fusion;
using System;

public class ChatManager : NetworkBehaviour {
    public static ChatManager Instance { get; private set; }
    public event Action<string> OnMessageReceived;

    private void Awake() {
        // Singleton
        if (Instance == null) Instance = this;
        else Runner.Despawn(Object);
    }

    public void SendChat(string rawMessage) { 
        if(!Object.HasInputAuthority) {
            return;
        }
        if(string.IsNullOrWhiteSpace(rawMessage)) {
            return;
        }
        string playerName = PlayerNameStorage.GetPlayerName();
        string finalMessage = $"<b><{playerName}>:</b> {rawMessage}";
        RPC_SendToServer(finalMessage);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SendToServer(string message) { 
        if(string.IsNullOrWhiteSpace(message)) {
            return;
        }
        RPC_Broadcast(message);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Broadcast(string message) {
        OnMessageReceived?.Invoke(message);
    }
}
