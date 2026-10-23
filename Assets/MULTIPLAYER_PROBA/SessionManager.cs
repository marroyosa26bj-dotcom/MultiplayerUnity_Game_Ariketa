using UnityEngine;
using Unity.Services.Core;
using System;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.Serialization;

public class SessionManager : MonoBehaviour
{
    [FormerlySerializedAs("salaCodigo")]
    [SerializeField] private TMP_Text kodea;
    [FormerlySerializedAs("campoCodigo")]
    [SerializeField] private TMP_InputField joinCode;

    private Task initializationTask;

    private async void Start()
    {
        try
        {
            await EnsureServicesInitializedAsync();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    public async Task StartSessionAsHost()
    {
        await EnsureServicesInitializedAsync();

        var options = new SessionOptions
        {
            MaxPlayers = 10
        }.WithRelayNetwork();

        var session = await MultiplayerService.Instance.CreateSessionAsync(options);

        if (kodea != null)
            kodea.text = session.Code;

        Debug.Log($"Session {session.Id} created! Join code: {session.Code}");
    }

    public async Task StartSessionAsClient(string joinCode)
    {
        if (string.IsNullOrWhiteSpace(joinCode))
            throw new ArgumentException("A session join code is required.", nameof(joinCode));

        await EnsureServicesInitializedAsync();

        var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);
        Debug.Log($"Joined session {session.Id} with code {joinCode}");
    }

    public async void CrearSala()
    {
        try
        {
            await StartSessionAsHost();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    public async void UnirseSala()
    {
        if (joinCode == null)
        {
            Debug.LogError("SessionManager: Join code input is not assigned.", this);
            return;
        }

        try
        {
            string code = joinCode.text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                Debug.LogError("SessionManager: Enter a session join code before joining.", this);
                return;
            }

            await StartSessionAsClient(code);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private async Task EnsureServicesInitializedAsync()
    {
        if (initializationTask == null)
            initializationTask = InitializeServicesAsync();

        try
        {
            await initializationTask;
        }
        catch
        {
            initializationTask = null;
            throw;
        }
    }

    private static async Task InitializeServicesAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log($"Signed in anonymously. Player ID: {AuthenticationService.Instance.PlayerId}");
    }

}