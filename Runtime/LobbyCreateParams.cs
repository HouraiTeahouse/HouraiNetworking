
namespace HouraiTeahouse.Networking {

public enum LobbyType {
  Private = 0,
  Public = 1
}

public struct LobbyCreateParams {
  public LobbyType Type;
  public uint Capacity;
}

}
