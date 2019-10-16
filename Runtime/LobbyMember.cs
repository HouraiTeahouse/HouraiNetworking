namespace HouraiTeahouse.Networking {

public readonly struct LobbyMember : IMetadataContainer {

  public AccountHandle AccountID { get; }
  public LobbyBase Lobby { get; }

  public event Action<byte[]> OnNetworkMessage;

  public LobbyMember(IIntegrationLobby lobby, uint userId) {
    AccountID = new IntegrationAccountHandle(userId);
    Lobby = lobby;
  }

  public void SendNetworkMessage(byte[] msg,
                                 Reliabilty reliabilty = Reliabilty.Reliable) {
    Lobby.SendNetworkMessage(AccountID, msg, reliabilty: reliabilty);
  }

  internal DispatchNetworkMessage(byte channel, byte[] msg) {
    OnNetworkMessage?.Invoke(channel, msg);
  }

}

}
