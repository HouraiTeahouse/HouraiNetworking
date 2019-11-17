using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HouraiTeahouse.Networking {

public class LocalLobbyTests {

    [Test]
    public void LocalLobby_Create() {
        const ulong id = 69;
        const ulong ownerId = 400;
        const int capacity = 4;
        var lobbyView = LocalLobby.Create(id, capacity, ownerId);
        Assert.AreEqual(id, lobbyView.Id, "Lobby ID must be the same");
        Assert.AreEqual(ownerId, lobbyView.OwnerId, "Lobby view ID must be the same");
        Assert.AreEqual(ownerId, lobbyView.UserId, "Lobby view user ID must be the same");
        Assert.AreEqual(1, lobbyView.MemberCount, "Lobby view user ID must be the same");
        Assert.AreEqual(capacity, lobbyView.Capacity, "Lobby view capacity must be the same");
    }

    [Test]
    public void LocalLobby_CreateViewDoesNotTriggerEvents() {
        const ulong id = 69;
        const ulong ownerId = 400;
        const int capacity = 4;
        var lobbyView = LocalLobby.Create(id, capacity, ownerId);
        lobbyView.OnMemberJoin += (member) => Assert.Fail("OnMemberJoin should not be called");
        lobbyView.OnMemberLeave += (member) => Assert.Fail("OnMemberLeave should not be called");
        lobbyView.BaseLobby.CreateView(450);
    }

    [Test]
    public void LocalLobby_JoinTriggersOnMemberJoin() {
        const ulong id = 69;
        const ulong ownerId = 400;
        const int capacity = 4;
        var lobbyView = LocalLobby.Create(id, capacity, ownerId);
        var triggered = false;
        lobbyView.OnMemberJoin += (member) => triggered = true;
        lobbyView.OnMemberLeave += (member) => Assert.Fail("OnMemberLeave should not be called");
        var remote = lobbyView.BaseLobby.CreateView(450);
        remote.Join().Wait();
        Assert.IsTrue(triggered);
    }

    [Test]
    public void LocalLobby_LeaveTriggersOnMemberLeave() {
        const ulong id = 69;
        const ulong ownerId = 400;
        const int capacity = 4;
        var lobbyView = LocalLobby.Create(id, capacity, ownerId);
        var triggered = false;
        lobbyView.OnMemberLeave += (member) => triggered = true;
        var remote = lobbyView.BaseLobby.CreateView(450);
        remote.Join().Wait();
        remote.Leave();
        Assert.IsTrue(triggered);
    }

    [Test]
    public void LocalLobby_SetMetadataUpdatesEveryone() {
        const ulong id = 69;
        const ulong ownerId = 400;
        const int capacity = 4;
        const string key = "key";
        const string value = "value";
        var lobbyView = LocalLobby.Create(id, capacity, ownerId);
        var triggered = false;
        lobbyView.OnUpdated += () => triggered = true;
        var remote = lobbyView.BaseLobby.CreateView(450);
        remote.Join().Wait();
        lobbyView.SetMetadata(key, value);
        Assert.AreEqual(value, remote.GetMetadata(key));
        Assert.IsTrue(triggered);
    }

    [Test]
    public void LocalLobby_SetMetadataDoesntUpdate() {
        const ulong id = 69;
        const ulong ownerId = 400;
        const int capacity = 4;
        const string key = "key";
        const string value = "value";
        var lobbyView = LocalLobby.Create(id, capacity, ownerId);
        var triggered = false;
        var remote = lobbyView.BaseLobby.CreateView(450);
        remote.Join().Wait();
        lobbyView.SetMetadata(key, value);
        lobbyView.OnUpdated += () => triggered = true;
        lobbyView.SetMetadata(key, value);
        Assert.AreEqual(value, remote.GetMetadata(key));
        Assert.IsFalse(triggered);
    }

    [Test]
    public void LocalLobby_DeleteMetadataUpdatesEveryone() {
        const ulong id = 69;
        const ulong ownerId = 400;
        const int capacity = 4;
        const string key = "key";
        const string value = "value";
        var lobbyView = LocalLobby.Create(id, capacity, ownerId);
        var triggered = false;
        lobbyView.OnUpdated += () => triggered = true;
        var remote = lobbyView.BaseLobby.CreateView(450);
        remote.Join().Wait();
        lobbyView.SetMetadata(key, value);
        lobbyView.DeleteMetadata(key);
        Assert.AreEqual(string.Empty, remote.GetMetadata(key));
        Assert.IsTrue(triggered);
    }

    [Test]
    public void LocalLobby_DeleteMetadataDoesntUpdate() {
        const ulong id = 69;
        const ulong ownerId = 400;
        const int capacity = 4;
        const string key = "key";
        const string value = "value";
        var lobbyView = LocalLobby.Create(id, capacity, ownerId);
        var triggered = false;
        lobbyView.OnUpdated += () => triggered = true;
        var remote = lobbyView.BaseLobby.CreateView(450);
        remote.Join().Wait();
        lobbyView.DeleteMetadata(key);
        Assert.AreEqual(string.Empty, remote.GetMetadata(key));
        Assert.IsFalse(triggered);
    }

    [Test]
    public void LocalLobby_SetMemberMetadataUpdatesEveryone() {
        const ulong id = 69;
        const ulong ownerId = 400;
        const int capacity = 4;
        const string key = "key";
        const string value = "value";
        var lobbyView = LocalLobby.Create(id, capacity, ownerId);
        ulong updatedId = 0;
        lobbyView.OnMemberUpdated+= (mem) => updatedId = mem.Id;
        var remote = lobbyView.BaseLobby.CreateView(450);
        remote.Join().Wait();
        var member = lobbyView.Members.Get(remote.UserId);
        member.SetMetadata(key, value);
        Assert.AreEqual(value, remote.Members.Get(remote.UserId).GetMetadata(key));
        Assert.AreEqual(updatedId, remote.UserId);
    }

    [Test]
    public void LocalLobby_DeleteMemberMetadataUpdatesEveryone() {
        const ulong id = 69;
        const ulong ownerId = 400;
        const int capacity = 4;
        const string key = "key";
        const string value = "value";
        var lobbyView = LocalLobby.Create(id, capacity, ownerId);
        ulong updatedId = 0;
        lobbyView.OnMemberUpdated+= (mem) => updatedId = mem.Id;
        var remote = lobbyView.BaseLobby.CreateView(450);
        remote.Join().Wait();
        var member = lobbyView.Members.Get(remote.UserId);
        member.SetMetadata(key, value);
        member.DeleteMetadata(key);
        Assert.AreEqual(string.Empty, remote.Members.Get(remote.UserId).GetMetadata(key));
        Assert.AreEqual(updatedId, remote.UserId);
    }

}

}
