using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HouraiTeahouse.Networking {

/// <summary>
/// A Lobby implementation that does not make any networked calls.
/// 
/// Unlike other lobby implementations, this does not do any permissions checking
/// when calling metadata mutation calls.
/// 
/// Primary use of this is for testing, but can also be used for establishing
/// local play using netplay-only codebase.
/// 
/// See also: <seealso cref="HouraiTeahouse.Networking.LocalLobby"/>
/// </summary>
public sealed class LocalLobbyView : Lobby {

    /// <summary>
    /// The underlying LocalLobby.
    /// </summary>
    public LocalLobby BaseLobby { get; }

    public override ulong Id { get; }
    public override ulong UserId { get; }
    public override ulong OwnerId => BaseLobby.OwnerId;
    public override LobbyType Type => BaseLobby.Type;
    public override int Capacity {
        get => BaseLobby.Capacity;
        set => BaseLobby.Capacity = value; 
    }

    internal LocalLobbyView(ulong userId, LocalLobby lobby) : base() {
        UserId = userId;
        BaseLobby = lobby;
        Members.Refresh();
    }

    public override int MemberCount => Members.Count;
    internal override IEnumerable<AccountHandle> GetMemberIds() => BaseLobby.GetMemberIds();
    public override string GetMetadata(string key) => BaseLobby.GetMetadata(key);
    public override void SetMetadata(string key, string value) => BaseLobby.SetMetadata(key, value);
    public override void DeleteMetadata(string key) => BaseLobby.DeleteMetadata(key);
    public override IReadOnlyDictionary<string, string> GetAllMetadata() => BaseLobby.GetAllMetadata();

    internal override string GetMemberMetadata(AccountHandle handle, string key) =>
        throw new NotSupportedException();
    internal override void SetMemberMetadata(AccountHandle handle, string key, string value) =>
        throw new NotSupportedException();
    internal override void DeleteMemberMetadata(AccountHandle handle, string key) =>
        throw new NotSupportedException();

    public override void SendLobbyMessage(FixedBuffer message) {
        BaseLobby.SendLobbyMessage(new AccountHandle(UserId), message);
    }

    internal override void SendNetworkMessage(AccountHandle target, FixedBuffer message, 
                                              Reliability reliability = Reliability.Reliable) {
        BaseLobby.SendNetworkMessage(new AccountHandle(UserId), target, message, reliability);
    }

    public override Task Join() {
        BaseLobby.Connect(this);
        return Task.CompletedTask;
    }

    public override void Leave() {
        BaseLobby.Disconnect(this);
        Dispose();
    }

    public override void Delete() => Dispose();

}

}
