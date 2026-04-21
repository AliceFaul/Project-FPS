using UnityEngine;
using Fusion;
using TMPro;

public class PlayerInfo : NetworkBehaviour {
    [SerializeField] private TMP_Text playerNameText;

    private PlayerNetworkSetup _setup;
    private string _lastRenderedName;

    public override void Spawned() {
        _setup = GetComponent<PlayerNetworkSetup>();
        RefreshPlayerNameText();
    }

    public override void Render() {
        RefreshPlayerNameText();
    }

    private void RefreshPlayerNameText() {
        if(playerNameText == null || _setup == null || Object == null) {
            return;
        }

        if(!_setup.TryGetPlayerMetaData(Object.InputAuthority, out var metaData)) {
            return;
        }

        string resolvedName = metaData.Name.ToString();
        if(string.IsNullOrWhiteSpace(resolvedName)) {
            resolvedName = PlayerNameStorage.DefaultPlayerName;
        }

        if(_lastRenderedName == resolvedName) {
            return;
        }

        _lastRenderedName = resolvedName;
        playerNameText.text = resolvedName;
    }
}
