using System;
using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

/// <summary>
/// An object for creating a local implementation of HouraiNetworking lobbies.
/// 
/// Note that this class doesn't actually derive from Lobby: that would be 
/// LocalLobbyView.
/// 
/// Primary use of this is for testing, but can also be used for establishing
/// local play using netplay-only codebase.
/// </summary>
public sealed class LocalLobby : IDisposable {

    readonly MetadataContainer _metadata;
    readonly Dictionary<AccountHandle, LocalLobbyView> _connectedViews;
    readonly double _packetLossPercent;
    readonly Random _packetLossRng;

    public ulong Id { get; }
    public LobbyType Type { get; set; }
    public ulong OwnerId { get; private set; }
    public int Capacity { get; set; }

    /// <summary>
    /// Creates a local lobby and provides you the connected owner's lobby.
    /// </summary>
    /// <param name="id">the lobby's ID</param>
    /// <param name="capacity">how many player can connect.</param>
    /// <param name="ownerId">the owner of the lobby.</param>
    /// <param name="packetLossPercent">for unreliable packets, how often </param>
    /// <param name="packetLossRng">a System.Random instance for randomly producing packet loss</param>
    public static LocalLobbyView Create(ulong id, int capacity, ulong ownerId = 0, 
                                        double packetLossPercent = 0, Random packetLossRng = null) {
        packetLossRng = packetLossRng ?? new Random();
        var lobby = new LocalLobby(id, capacity, ownerId, packetLossPercent, packetLossRng);
        LocalLobbyView owner = lobby.CreateView(ownerId);
        owner.Join().Wait();
        return owner;
    }

    LocalLobby(ulong id, int capacity, ulong ownerId, 
               double packetLossPercent, Random packetLossRng) {
        Id = id;
        OwnerId = ownerId;
        Capacity = capacity;
        _packetLossRng = packetLossRng;
        _connectedViews = new Dictionary<AccountHandle, LocalLobbyView>();
        _metadata = new MetadataContainer();
    }

    /// <summary>
    /// Creates a view of the lobby, as seen from a remote player.
    /// 
    /// This does not connect the view to the lobby. LobbyView.Join must be called
    /// to "connect" the view to to the lobby.
    /// </summary>
    /// <param name="id">the user to create the view from</param>
    /// <returns>the Lobby view from the view of that player</returns>
    public LocalLobbyView CreateView(AccountHandle id) {
        if (_connectedViews.TryGetValue(id, out LocalLobbyView view)) {
            return view;
        }
        return new LocalLobbyView(id, this);
    }

    /// <summary>
    /// Deletes the lobby. This will invalidate the state of all related
    /// LocalLobbyViews and the associated LobbyMembers.
    /// </summary>
    public void Delete() => Dispose();

    internal void Connect(LocalLobbyView view) {
        if (_connectedViews.Count >= Capacity) {
            throw new InvalidOperationException("Cannot join a lobby that is already full");
        }
        var handle = new AccountHandle(view.UserId);
        if (_connectedViews.ContainsKey(handle)) return;
        _metadata.AddMember(handle);
        _connectedViews[handle] = view;
        foreach (var connectedView in _connectedViews.Values) {
            connectedView.Members.Add(handle);
        }
    }

    internal void Disconnect(LocalLobbyView view) {
        var handle = new AccountHandle(view.UserId);
        if (!_connectedViews.ContainsKey(handle)) return;
        _metadata.RemoveMember(handle);
        _connectedViews.Remove(handle);
        foreach (var connectedView in _connectedViews.Values) {
            connectedView.Members.Remove(handle);
        }
    }
 
    internal void SendLobbyMessage(AccountHandle source, FixedBuffer msg) {
        foreach (var view in _connectedViews.Values) {
            if (view.Members.TryGetValue(source, out LobbyMember member)) {
                view.DispatchLobbyMessage(member, msg);
            }
        }
    }
 
    internal void SendNetworkMessage(AccountHandle source, AccountHandle target, FixedBuffer msg, Reliability reliability) {
        // Simulate unreliablity
        if (reliability == Reliability.Unreliable && _packetLossRng.NextDouble() > _packetLossPercent) {
            return;
        }
        LocalLobbyView view;
        LobbyMember member;
        if (!_connectedViews.TryGetValue(target, out view) ||
            !view.Members.TryGetValue(source, out member)) {
            return;
        }
        member.DispatchNetworkMessage(msg);
    }

    internal IEnumerable<AccountHandle> GetMemberIds() => _connectedViews.Keys;
    internal string GetMetadata(string key) => _metadata.GetMetadata(key);

    internal void SetMetadata(string key, string value) {
        if (_metadata.SetMetadata(key, value)) {
            DispatchUpdate();
        }
    }

    internal void DeleteMetadata(string key) {
        if (_metadata.DeleteMetadata(key)) {
            DispatchUpdate();
        }
    }

    internal string GetMemberMetadata(AccountHandle handle, string key) => 
        _metadata.GetMemberMetadata(handle, key);

    internal void SetMemberMetadata(AccountHandle handle, string key, string value) {
        if (_metadata.SetMemberMetadata(handle, key, value)) {
            DispatchMemberUpdate(handle);
        }
    }

    internal void DeleteMemberMetadata(AccountHandle handle, string key) {
        if (_metadata.DeleteMemberMetadata(handle, key)) {
            DispatchMemberUpdate(handle);
        }
    }

    internal IReadOnlyDictionary<string, string> GetAllMetadata() => _metadata.GetAllMetadata();

    void DispatchUpdate() {
        foreach (var view in _connectedViews.Values) {
            view.DispatchUpdate();
        }
    }

    void DispatchMemberUpdate(AccountHandle handle) {
        foreach (var view in _connectedViews.Values) {
            if (view.Members.TryGetValue(handle, out LobbyMember member)) {
                member.DispatchUpdate();
            }
        }
    }

    public void Dispose() {
        foreach (var view in _connectedViews.Values) {
            view.Dispose();
        }
    }

}

}
