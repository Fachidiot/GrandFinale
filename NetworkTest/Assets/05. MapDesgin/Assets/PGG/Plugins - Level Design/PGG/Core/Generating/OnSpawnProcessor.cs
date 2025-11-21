using UnityEngine;

namespace FIMSpace.Generating
{
    /// <summary>
    /// Can be assigned in the FieldSetup to proceed operations on every single spawned object by the FieldSetup
    /// </summary>
    public abstract class OnSpawnProcessor : ScriptableObject
    {
        /// <summary> Called when instantiating for target FieldSetup is starting </summary>
        public virtual void OnInstantiationBegin(FGenGraph<FieldCell, FGenPoint> grid, Transform container) { }

        /// <summary> Called when object is instantiated on the scene in the grid cell </summary>
        public abstract void OnInstantiateObject(SpawnData spawn, GameObject spawned, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, Transform container);
        
        /// <summary> Extra generated objects by IGenerators like ObjectStampEmitter. Here SpawnData spawn is parent of the additional instanted objects </summary>
        public virtual void OnInstantiateAdditionalObject(SpawnData spawn, GameObject spawned, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, Transform container) { }
        
        /// <summary> Called when instantiation for target FieldSetup ended </summary>
        public virtual void OnInstantiationCompleted(FGenGraph<FieldCell, FGenPoint> grid, Transform container) { }
    }
}