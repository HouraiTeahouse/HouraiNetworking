using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HouraiTeahouse.Networking {

public interface IMessageProcessor {

    void Apply(ref ReadOnlySpan<byte> buffer);
    void Unapply(ref ReadOnlySpan<byte> buffer);

}

}
