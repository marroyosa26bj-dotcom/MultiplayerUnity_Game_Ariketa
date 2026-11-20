using UnityEngine;
using Unity.Services.Core;
using System;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using System.Threading.Tasks;
using TMPro;

public class SessionManager : MonoBehaviour
{
    [SerializeField] private TMP_Text kodea;
    private Task servicesInitialization;

    private Task EnsureServicesInitializedAsync()
    {
        if (servicesInitialization == null)
            servicesInitialization = InitializeServicesAsync();

        return servicesInitialization;
    }

    private async Task InitializeServicesAsync()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log($"Sign in anonymously succeeded! PlayerID: {AuthenticationService.Instance.PlayerId}");
    }

    public async Task StartSessionAsHost()
    {
        if (kodea == null)
            throw new InvalidOperationException("SessionManager: Join-code text is not assigned.");

        await EnsureServicesInitializedAsync();

        var options = new SessionOptions
        {
            MaxPlayers = 10
        }.WithRelayNetwork(); // or WithDistributedAuthorityNetwork() to use Distributed Authority instead of Relay
        
        var session = await MultiplayerService.Instance.CreateSessionAsync(options);
        kodea.text = session.Code;
        Debug.Log($"Session {session.Id} created! Join code: {session.Code}");
    }

    public async Task StartSessionAsClient(string joinCode)
    {
        if (string.IsNullOrWhiteSpace(joinCode))
            throw new ArgumentException("A join code is required.", nameof(joinCode));

        await EnsureServicesInitializedAsync();

        var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);
        Debug.Log($"Joined session {session.Id} with code {joinCode}");
    } 

}