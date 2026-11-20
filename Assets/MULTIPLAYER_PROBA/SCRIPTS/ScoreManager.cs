
using System;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct PlayerScoreEntry : INetworkSerializable, IEquatable<PlayerScoreEntry>
{
    public ulong ClientId;
    public FixedString64Bytes Name;
    public int Score;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Score);
    }

    public bool Equals(PlayerScoreEntry other)
    {
        return ClientId == other.ClientId &&
               Name.Equals(other.Name) &&
               Score == other.Score;
    }
}

public class ScoreManager : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    public static ScoreManager Instance { get; private set; }

    public NetworkVariable<int> TotalScore = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkList<PlayerScoreEntry> PlayerScores = new();

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Ya existe otro ScoreManager.");
            return;
        }

        Instance = this;

        TotalScore.OnValueChanged += OnScoreChanged;
        UpdateText(TotalScore.Value);
    }

    public override void OnNetworkDespawn()
    {
        TotalScore.OnValueChanged -= OnScoreChanged;

        if (Instance == this)
            Instance = null;
    }

    private void OnScoreChanged(int oldValue, int newValue)
    {
        UpdateText(newValue);
    }

    private void UpdateText(int value)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {value}";
    }

    public void AddScore(int amount)
    {
        if (!IsServer || amount <= 0)
            return;

        TotalScore.Value += amount;
    }

    public void AddPlayerPoint(ulong clientId, string playerName)
    {
        if (!IsServer || string.IsNullOrWhiteSpace(playerName))
            return;

        for (int i = 0; i < PlayerScores.Count; i++)
        {
            PlayerScoreEntry entry = PlayerScores[i];
            if (entry.ClientId != clientId)
                continue;

            entry.Name = playerName;
            entry.Score++;
            PlayerScores[i] = entry;
            AddScore(1);
            return;
        }

        PlayerScores.Add(new PlayerScoreEntry
        {
            ClientId = clientId,
            Name = playerName,
            Score = 1
        });
        AddScore(1);
    }
}