namespace HouraiTeahouse.Networking {

public struct ConnectionStats {

    public uint PacketsSent;
    public uint PacketsRecieved;

    public ulong BytesSent;
    public ulong BytesRecieved;

    public static ConnectionStats operator +(in ConnectionStats a, in ConnectionStats b) {
        return new ConnectionStats{
            PacketsSent = a.PacketsSent + b.PacketsSent,
            PacketsRecieved = a.PacketsRecieved + b.PacketsRecieved,
            BytesSent = a.BytesSent + b.BytesSent,
            BytesRecieved = a.BytesRecieved + b.BytesRecieved
        };
    }

}

}