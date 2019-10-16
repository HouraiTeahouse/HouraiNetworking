
namespace HouraiTeahouse.Networking {

public enum LobbyType {
  Private, Public
}

public struct LobbyCreateParams {
  public LobbyType Type;
  public uint Capacity;
}

}
