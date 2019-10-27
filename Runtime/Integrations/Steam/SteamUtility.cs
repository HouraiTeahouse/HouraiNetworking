using Steamworks;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace HouraiTeahouse.Networking.Steam {

public static class SteamUtility {

  /// <summary>
  /// Creates error logs for a given error code.
  /// Does nothing if there is no error.
  /// </summary>
  /// <param name="result">the error code of the error.</param>
  public static void LogIfError(EResult result) {
    if (result == EResult.k_EResultOK) return;
    Debug.LogError($"Steam Error: {result}");
  }

  /// <summary>
  /// Creates an exception out of a Steam Result.
  /// </summary>
  /// <param name="result">the error code of the error.</param>
  /// <returns>an exception representing the error, null if no error.</returns>
  public static Exception ThrowIfError(EResult result) {
    if (result == EResult.k_EResultOK) return null;
    throw new Exception($"Steam Error: {result}");
  }

  public static async Task<T> ToTask<T>(this SteamAPICall_t apiCall) {
    var completionSource = new TaskCompletionSource<T>();
    CallResult<T>.Create((callResult, failure) => {
      if (failure) {
        completionSource.TrySetException(new Exception("Steam Networking Exception."));
      } else {
        completionSource.TrySetResult(callResult);
      }
    }).Set(apiCall);
    return await completionSource.Task;
  }

  public static async Task<T> WaitFor<T>() {
    var completionSource = new TaskCompletionSource<T>();
    Callback<T>.Create(result => completionSource.TrySetResult(result));
    return await completionSource.Task;
  }

}

}
