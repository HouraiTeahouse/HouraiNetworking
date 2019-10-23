using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HouraiTeahouse.Networking {

public interface IMessageProcessor {

    void Apply(ref byte[] data, ref int size);
    void Unapply(ref byte[] data, ref int size);

}

}