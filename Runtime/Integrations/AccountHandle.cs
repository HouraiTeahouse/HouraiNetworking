namespace HouraiTeahouse.Networking {

public readonly struct AccountHandle {

  public readonly ulong Id;

  public AccountHandle(ulong accountId) {
    Id = accountId;
  }

  public static implicit operator ulong(AccountHandle handle) => handle.Id;
  public static implicit operator AccountHandle(ulong id) => new AccountHandle(id);

  public override string ToString() =>
    Id.ToString();

}

}
