namespace HouraiTeahouse.Networking {

public readonly struct AccountHandle {

  public readonly uint AccountID;

  public AccountHandle(uint accountId) {
    AccountID = AccountID;
  }

  public static implicit operator uint(AccountHandle handle) => handle.AccountID;
  public static implicit operator AccountHandle(uint id) =>
    new AccountHandle(id);

}

}
