using System;
using System.Threading.Tasks;
using UnityEngine;
using DiscordApp = Discord;

namespace HouraiTeahouse.Networking.Discord {

public static class DiscordUtility {

    public static Exception ToError(DiscordApp.Result result) {
        if (result == DiscordApp.Result.Ok) return null;
        return new Exception($"Discord Error: {result}");
    }

    public static void RaiseIfError(DiscordApp.Result result) {
        Exception error = ToError(result);
        if (error != null) throw error;
    }

    public static bool CancelIfError<T>(TaskCompletionSource<T> source, 
                                 DiscordApp.Result result) {
        Exception error = ToError(result);
        if (error != null) source.SetException(error);
        return error != null;
    }

    public static void LogIfError(DiscordApp.Result result) {
        Exception error = ToError(result);
        if (error != null) {
            Debug.LogException(error);
        }
    }


}

}