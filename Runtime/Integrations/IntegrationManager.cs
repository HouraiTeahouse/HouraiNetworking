using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HouraiTeahouse.Networking;
using HouraiTeahouse.Networking.Steam;
using HouraiTeahouse.Networking.Discord;

namespace HouraiTeahouse.Networking {

public class IntegrationManager : MonoBehaviour {

    public static IntegrationManager Instance { get; private set; }

#pragma warning disable 0649
    [Serializable]
    abstract class IntegrationConfig {
        public abstract string IntegrationName { get; }
        public bool Enabled;
        public abstract IIntegrationClient CreateClient();
    }

    [Serializable]
    class SteamConfig : IntegrationConfig {
        public uint AppId;
        public override string IntegrationName => "Steam";
        public override IIntegrationClient CreateClient() =>
            new SteamIntegrationClient(AppId);
    }

    [Serializable]
    class DiscordConfig : IntegrationConfig {
        public override string IntegrationName => "Discord";
        public long ClientId;
        public override IIntegrationClient CreateClient() =>
            new DiscordIntegrationClient(ClientId);
    }

    enum UpdateMode {
        FixedUpdate, Update,  Manual
    }

    public IReadOnlyCollection<IIntegrationClient> Integrations { get; private set; }

    [SerializeField] UpdateMode _updateMode;
    [SerializeField] SteamConfig _steam;
    [SerializeField] DiscordConfig _discord;
#pragma warning restore 0649

    IIntegrationClient[] _integrations;

    /// <summary>
    /// Called upon object initialization.
    /// </summary>
    void Awake() {
        Instance = this;
        var configs = new IntegrationConfig[] { _steam, _discord };
        var integrations = new List<IIntegrationClient>();
        foreach (var config in configs) {
            if (!config.Enabled) continue;
            try {
                var client = config.CreateClient();
                integrations.Add(client);
                Debug.Log($"Initialized integration: {config.IntegrationName}");
            } catch (Exception e) {
                Debug.LogError($"Failed to load integration ({config.IntegrationName}:\n{e}");
            }
        }
        _integrations = integrations.ToArray();
        Integrations = Array.AsReadOnly(_integrations);
    }

    /// <summary>
    /// Called before every frame.
    /// </summary>
    void Update() {
        if (_updateMode != UpdateMode.Update) return;
        RunIntegrationUpdates();
    }

    /// <summary>
    /// Called at a fixed rate.
    /// </summary>
    void FixedUpdate() {
        if (_updateMode != UpdateMode.FixedUpdate) return;
        RunIntegrationUpdates();
    }

    public T GetIntegration<T>() => Integrations.OfType<T>().FirstOrDefault();

    public void RunIntegrationUpdates() {
        foreach (var integration in _integrations) {
            integration.Update();
        }
    }

    void OnDestroy() {
        foreach (var integration in _integrations) {
            try {
                (integration as IDisposable)?.Dispose();
            } catch (Exception e) {
                Debug.LogError($"Failed to shutdown integration {integration.GetType()}: {e}");
            }
        }
    }

}

}
