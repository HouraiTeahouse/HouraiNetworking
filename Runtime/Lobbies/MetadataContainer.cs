using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

/// <summary>
/// A metadata container for use in local and LAN based lobbies.
/// </summary>
internal sealed class MetadataContainer {

    readonly Dictionary<string, string> _metadata;
    readonly Dictionary<AccountHandle, Dictionary<string, string>> _memberMetadata;

    public MetadataContainer() {
        _metadata = new Dictionary<string, string>();
        _memberMetadata = new Dictionary<AccountHandle, Dictionary<string, string>>();
    }

    /// <summary>
    /// Initializes a lobby member's metadata.
    /// </summary>
    /// <param name="handle">the account handle of the lobby member.</param>
    public void AddMember(AccountHandle handle) {
        if (_memberMetadata.ContainsKey(handle)) return;
        _memberMetadata.Add(handle, new Dictionary<string, string>());
    }

    /// <summary>
    /// Removes a lobby member's metadata.
    /// </summary>
    /// <param name="handle">the account handle of the lobby member.</param>
    public void RemoveMember(AccountHandle handle) => _memberMetadata.Remove(handle);

    /// <summary>
    /// Gets all lobby level metadata as a readonly dictionary.
    /// </summary>
    /// <returns>All lobby level metadata.</returns>
    public IReadOnlyDictionary<string, string> GetAllMetadata() => _metadata;

    /// <summary>
    /// Gets a lobby level metadata value for a key.
    /// </summary>
    /// <returns>the metadata value for the key, empty string if no value for the key is present</returns>
    public string GetMetadata(string key) {
        if (_metadata.TryGetValue(key, out string value)) {
            return value;
        }
        return string.Empty;
    }

    /// <summary>
    /// Set the lobby level metadata for a given key.
    /// 
    /// Note: this does not do any permissions checking.
    /// </summary>
    /// <param name="key">the metadata key to set</param>
    /// <param name="value">the metadata value to set</param>
    /// <returns>true if the value has changed, false otherwise.</returns>
    public bool SetMetadata(string key, string value) {
        bool changed = GetMetadata(key) != value;
        _metadata[key] = value;
        return changed;
    }

    /// <summary>
    /// Delete the lobby level metadata  for a given key.
    /// 
    /// Note: this does not do any permissions checking.
    /// </summary>
    /// <param name="key">the metadata key to delete</param>
    /// <returns>true if the value was deleted, false otherwise.</returns>
    public bool DeleteMetadata(string key) {
        bool changed = _metadata.ContainsKey(key);
        _metadata.Remove(key);
        return changed;
    }

    /// <summary>
    /// Gets a member level metadata value for a key.
    /// </summary>
    /// <param name="handle">the account handle of the lobby member.</param>
    /// <returns>the metadata value for the key, empty string if no value for the key is present</returns>
    public string GetMemberMetadata(AccountHandle handle, string key) {
        Dictionary<string, string> memberMetadata;
        string value;
        if (!_memberMetadata.TryGetValue(handle, out memberMetadata) ||
            !memberMetadata.TryGetValue(key, out value)) {
            return string.Empty;
        }
        return value;
    }

    /// <summary>
    /// Set the member level metadata for a given key.
    /// 
    /// Note: this does not do any permissions checking.
    /// </summary>
    /// <param name="handle">the account handle of the lobby member.</param>
    /// <param name="key">the metadata key to set</param>
    /// <param name="value">the metadata value to set</param>
    /// <returns>true if the value has changed, false otherwise.</returns>
    public bool SetMemberMetadata(AccountHandle handle, string key, string value) {
        Dictionary<string, string> memberMetadata;
        if (_memberMetadata.TryGetValue(handle, out memberMetadata)) {
            bool changed = true;
            if (memberMetadata.TryGetValue(key, out string currentValue)) {
                changed = currentValue == value;
            }
            memberMetadata[key] = value;
            return changed;
        }
        return false;
    }

    /// <summary>
    /// Delete the member level metadata  for a given key.
    /// 
    /// Note: this does not do any permissions checking.
    /// </summary>
    /// <param name="key">the metadata key to delete</param>
    /// <returns>true if the value was deleted, false otherwise.</returns>
    public bool DeleteMemberMetadata(AccountHandle handle, string key) {
        Dictionary<string, string> memberMetadata;
        if (_memberMetadata.TryGetValue(handle, out memberMetadata)) {
            bool changed = memberMetadata.ContainsKey(key);
            memberMetadata.Remove(key);
            return changed;
        }
        return false;
    }

}

}
