namespace HouraiTeahouse.Networking {

/// <summary>
/// A platform agnostic unique identifier for a user.
/// 
/// AccountHandles generated from one platform will likely not
/// be valid for another platform.
/// 
/// It is implicitly convertible to and from UInt64s.
/// </summary>
public readonly struct AccountHandle {

  /// <summary>
  /// The underlying UInt64 identifier.
  /// </summary>
  public readonly ulong Id;

  public AccountHandle(ulong accountId) {
    Id = accountId;
  }

  public static implicit operator ulong(AccountHandle handle) => handle.Id;
  public static implicit operator AccountHandle(ulong id) => new AccountHandle(id);

  public override string ToString() => Id.ToString();

}

}
