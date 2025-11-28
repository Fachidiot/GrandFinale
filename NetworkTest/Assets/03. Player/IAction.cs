using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAction
{
    bool Value { get; }

    void ReleaseAction();
}
