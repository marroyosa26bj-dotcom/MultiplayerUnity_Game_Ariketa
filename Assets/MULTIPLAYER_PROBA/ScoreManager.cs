
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance;

    public TMP_Text scoreText;

    private NetworkVariable<int> score = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        Instance = this;
        score.OnValueChanged += UpdateScore;
        UpdateScore(0, score.Value);
    }

    public override void OnNetworkDespawn()
    {
        score.OnValueChanged -= UpdateScore;
    }

    void UpdateScore(int oldScore, int newScore)
    {
        scoreText.text = "Puntuación: " + newScore;
    }

    [Rpc(SendTo.Server)]
    public void AddPointRpc()
    {
        score.Value++;
    }
}
