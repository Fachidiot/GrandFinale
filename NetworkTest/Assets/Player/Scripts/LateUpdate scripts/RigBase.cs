using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigBase : MonoBehaviour
{
    [Serializable]
    public class RigGroupObject
    {
        public RigGroup m_Object;
        public bool m_Active = true;
    }
    public bool rigActive = true;
    public List<RigGroupObject> RigGroupObjects = new List<RigGroupObject>();
    private List<OnRig> OnRigs = new List<OnRig>();
    private List<OnRig> returnedRigs = new List<OnRig>();
    private List<IUpdaterBeforeRig> beforeRigUpdateScripts = new List<IUpdaterBeforeRig>();

    private void Awake()
    {
        OnRigsInitialized();
    }

    void OnRigsInitialized()
    {
        foreach (var rigGroupObject in RigGroupObjects)
        {
            if (!rigGroupObject.m_Active | rigGroupObject.m_Object == null) continue;

            OnRigs.AddRange(rigGroupObject.m_Object.GetOnRigsList());
        }

        foreach (var rig in OnRigs)
        {
            if (rig.isReturnableToOriginalState) returnedRigs.Add(rig);
        }

        Transform _root = transform.root;
        var allScripts = _root.GetComponentsInChildren<MonoBehaviour>();
        foreach (var _script in allScripts)
        {
            if (_script is IUpdaterBeforeRig iUpdaterBeforeRig)
            {
                beforeRigUpdateScripts.Add(iUpdaterBeforeRig);
            }
        }
    }

    private void Update()
    {
        if (!rigActive) return;

        foreach (var onRig in returnedRigs)
            onRig.ReturnToOriginalState();

    }

    void LateUpdate()
    {
        if (!rigActive) return;

        foreach (var UpdaterBeforeRig in beforeRigUpdateScripts)
            UpdaterBeforeRig.UpdateBeforeRig();

        foreach (var onR in OnRigs)
        {
            if (onR == null) continue;
            onR.Execute();
        }
    }
}
