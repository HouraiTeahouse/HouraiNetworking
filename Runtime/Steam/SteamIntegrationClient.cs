using Steamworks;

namespace HouraiTeahouse.Networking.Steam {

public class SteamIntegrationClient : IIntegrationClient {

  static bool _initialized;
  static readonly SteamLobbyManager _lobbyManager;

  static SteamIntegrationClient() {
    _lobbyManager = new SteamLobbyManager();
  }

  public AccountHandle ActiveUser { get; private set; }
  public ILobbyManager LobbyManager => _lobbyManager;

  public SteamIntegrationClient() {
    if (_initialized) return;
    RunSanityChecks();
    if (!SteamAPI.Init()) {
      throw new Exception("SteamAPI_Init() failed. Refer to Valve's documentation for more information.")
    }
    _initialized = true;
    var _apiMessageWarningHook = new SteamAPIMessageWarningHook_t(DebugTextHook);
    SteamClient.SetWarningMessageHook(_apiMessageWarningHook);
  }

  public void Update() => SteamAPI.RunCallbacks();

  public void Dispose() => SteamAPI.Shutdown();

  static void RunSanityChecks() {
    if (!Packsize.Test()) {
        throw new Exception("Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.");
    }

    if (!DllCheck.Test()) {
        throw new Exception( "DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.");
    }
  }

  static void DebugTextHook(int nSeverity,
                            System.Text.StringBuilder pchDebugText) {
    Debug.LogWarning(pchDebugText);
  }

}

}
