using Steamworks;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace HouraiTeahouse.Networking.Steam {

public class SteamIntegrationClient : IIntegrationClient {

    static readonly SteamLobbyManager _lobbyManager;

    static SteamIntegrationClient() {
        _lobbyManager = new SteamLobbyManager();
    }

    public AccountHandle ActiveUser => new AccountHandle(SteamClient.SteamId);
    public ILobbyManager LobbyManager => _lobbyManager;

    public SteamIntegrationClient(uint appId) {
        if (!SteamClient.IsValid) {
            SteamClient.Init(appId, asyncCallbacks: false);
        }
    }

    public void Update() {
        Assert.IsTrue(SteamClient.IsValid);
        SteamClient.RunCallbacks();
        _lobbyManager.Update();
    }

    public void Dispose() => SteamClient.Shutdown();

}

}
