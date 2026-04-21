using UnityEngine;
using Fusion;
using TMPro;

public class PlayerInfo : NetworkBehaviour {
    [SerializeField] private TMP_Text playerNameText;

    private PlayerNetworkSetup _setup;
    private string _lastRenderedName;
    private Color _lastRenderedColor = Color.clear;

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
            Color targetColor = GetPlayerNameColor(Object.InputAuthority);
            if(_lastRenderedColor == targetColor) {
                return;
            }
        }
        _lastRenderedName = resolvedName;
        _lastRenderedColor = GetPlayerNameColor(Object.InputAuthority);
        playerNameText.text = resolvedName;
        playerNameText.color = _lastRenderedColor;
    }

    private static Color GetPlayerNameColor(PlayerRef playerRef) {
        int seed = Mathf.Abs(playerRef.RawEncoded);
        float hue = (seed * 0.61803398875f) % 1f;
        float saturation = 0.65f + ((seed % 3) * 0.1f);
        float value = 0.95f;
        return Color.HSVToRGB(hue, Mathf.Clamp01(saturation), value);
    }
}
