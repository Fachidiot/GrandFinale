using FIMSpace.Graph;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Operations
{

    public class PR_GetCellSize : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Get Cell Size"; }
        public override string GetNodeTooltipDescription { get { return "Returning Vector3 of planner's grid cell size in all axes."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(196, _EditorFoldout ? 108 : 81); } }
        public override bool IsFoldable => true;
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        [Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public PGGVector3Port OutVal;
        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGPlannerPort Planner;

        public override void OnStartReadingNode()
        {
            Planner.TriggerReadPort();
            var checker = Planner.Get_Checker;
            if (checker == null) checker = CurrentExecutingPlanner?.LatestChecker;
            if (checker == null) return;
            OutVal.Value = checker.RootScale;
        }


#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (sp == null) sp = baseSerializedObject.FindProperty("Planner");

            if (_EditorFoldout)
            {
                Planner.AllowDragWire = true;
                EditorGUILayout.PropertyField(sp);
            }
            else
            {
                Planner.AllowDragWire = false;
            }
        }


#endif
    }
}