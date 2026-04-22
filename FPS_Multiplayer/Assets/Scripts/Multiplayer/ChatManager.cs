using Fusion;
using System;
using UnityEngine;

public class ChatManager : NetworkBehaviour {
    public static ChatManager Instance { get; private set; }
    public event Action<string> OnMessageReceived;

    private void Awake() {
        // Singleton
        if (Instance == null) Instance = this;
        else Runner.Despawn(Object);
    }

    public void SendChat(string rawMessage) { 
        if(Runner == null || Object == null) {
            return;
        }
        if(string.IsNullOrWhiteSpace(rawMessage)) {
            return;
        }
        string playerName = PlayerNameStorage.GetPlayerName();
        Color playerNameColor = PlayerInfo.ResolvePlayerNameColor(Runner.LocalPlayer);
        string playerNameColorHex = ColorUtility.ToHtmlStringRGB(playerNameColor);
        string finalMessage = $"<color=#{playerNameColorHex}><b>{playerName}:</b></color> {rawMessage}";
        RPC_SendToServer(finalMessage);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
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
