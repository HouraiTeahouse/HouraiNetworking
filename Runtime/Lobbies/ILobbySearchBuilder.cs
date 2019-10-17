namespace HouraiTeahouse.Networking {

public enum SearchComparison {
  LessThanOrEqual    = -2,
  LessThan           = -1,
  Equal              = 0,
  GreaterThan        = 1,
  GreaterThanOrEqual = 2,
  NotEqual           = 3,
}

public enum DistanceFilter {
  Close = 1,
  Default = 2,
  Far = 3,
  Worldwide = 4,
}

public interface ILobbySearchBuilder {

  ILobbySearchBuilder Filter(string key, SearchComparison comparison, string value);
  ILobbySearchBuilder Sort(string key, string value);
  ILobbySearchBuilder Limit(int limit);

}

}
