using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public abstract class SpawnActionBase : ScriptableObject
    {
        public virtual void OnGameObjectSpawn(SpawnData spawn, GameObject spawned, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, Transform container, FieldSetup preset)
        { }

        public virtual void OnAfterAllObjectsSpawned(SpawnData spawn, GameObject spawned, FieldCell cell, List<IGenerating> generated, FGenGraph<FieldCell, FGenPoint> grid, Transform container, FieldSetup preset) 
        { }
    }
}