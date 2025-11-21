using UnityEngine;

namespace FIMSpace.Generating.Rules.QuickSolutions
{
    public class SR_CornerDoorwayCheck : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "Command Dir vs Rotation (Corner Doorway Check)"; }
        public override string Tooltip() { return "Checking command direction and calculated rotation to allow or not allow generating object in this position"; }
        public EProcedureType Type { get { return EProcedureType.Coded; } }
        [Space(5)]
        public Vector3 DesiredDirection = Vector3.forward;
        public Vector3 DesiredRotation = new Vector3(0, 90, 0);

        public override void CheckRuleOn(FieldModification mod, ref SpawnData spawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            CellAllow = true; // Allow on start, then disallow when conflict found

            if (restrictDirection == null) // No command direction
                return;

            if (Mathf.Abs(Vector3.Dot(DesiredDirection, restrictDirection.Value)) > 0.05f) // Too big angle difference for wanted direction and command direction
            {
                CellAllow = false;
                return;
            }

            // Strictly match current defined spawn rotation
            Vector3 modelDir = Quaternion.Euler(spawn.RotationOffset) * Vector3.forward;
            Vector3 modDesDir = Quaternion.Euler(DesiredRotation) * Vector3.forward;

            if (Mathf.Abs(Vector3.Dot(modelDir, modDesDir)) < 0.05f)
            {
                CellAllow = false;
            }
        }
    }

}