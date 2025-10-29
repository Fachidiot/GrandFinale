using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimatorParametersWindow : EditorWindow
{
    private StateMachineBehaviour _selectedState;
    public GameObject gameObject;

    private AnimationClip Clip
    {
        set => _modelPreview.animationClip = value;
    }

    private Editor _editor;
    private readonly EventsView _eventView = new EventsView();
    private readonly GUISplitWindow _guiSplitWindow = new GUISplitWindow();
    private readonly PlayTimeLinePanel _playablePanel = new PlayTimeLinePanel();
    private readonly ModelPreview _modelPreview = new ModelPreview();
    private readonly CurvesEventsView _curvesEventsView = new CurvesEventsView();
    private readonly AnimatorFloatCurvesView _curvesFloatsView = new AnimatorFloatCurvesView();
    private void OnFocus()
    {
        OnSelectionChange();
    }
    [MenuItem("Window/Animation/CustomAnimStateWindow")]
    static void Init()
    {
        AnimatorParametersWindow animationParametersWindow =
                (AnimatorParametersWindow)EditorWindow.GetWindow(typeof(AnimatorParametersWindow));
        animationParametersWindow.Show();
    }

    private void OnEnable()
    {
        _playablePanel.OnPlayTimeNormalized += _modelPreview.AnimationTimeUpdate;
        _playablePanel.OnPlayTimeNormalized += _eventView.TimeUpdate;
        _playablePanel.OnPlayTimeNormalized += _curvesEventsView.TimeUpdate;
        _playablePanel.OnPlayTimeNormalized += _curvesFloatsView.TimeUpdate;
        _curvesEventsView.OnKeyframeSelected += _playablePanel.SetTime;
        _curvesFloatsView.OnKeyframeSelected += _playablePanel.SetTime;

        OnSelectionChange();
        if (_selectedState != null)
        {
            ComponentsUpdate();
        }
    }
    private void OnDisable()
    {
        _playablePanel.OnPlayTimeNormalized -= _modelPreview.AnimationTimeUpdate;
        _playablePanel.OnPlayTimeNormalized -= _eventView.TimeUpdate;
        _playablePanel.OnPlayTimeNormalized -= _curvesEventsView.TimeUpdate;
        _playablePanel.OnPlayTimeNormalized -= _curvesFloatsView.TimeUpdate;
        _curvesEventsView.OnKeyframeSelected -= _playablePanel.SetTime;
        _curvesFloatsView.OnKeyframeSelected -= _playablePanel.SetTime;
    }

    void OnSelectionChange()
    {
        if (Selection.activeObject is AnimatorState animatorState)
        {
            Clip = animatorState.motion as AnimationClip;

            if (animatorState.behaviours.Length == 0)
            {
                _selectedState = null;
                return;
            }

            foreach (var animationStateBehaviour in animatorState.behaviours)
            {
                if (animationStateBehaviour is IAnimatorCurves |
                animationStateBehaviour is ICurveEventsList |
                animationStateBehaviour is IEventsLists)
                {
                    _selectedState = animationStateBehaviour;
                    ComponentsUpdate();
                    break;
                }
                else
                {
                    _selectedState = null;
                }
            }
        }
        else
        {
            _selectedState = null;
            Clip = null;
        }
    }

    void ComponentsUpdate()
    {
        _eventView.selectedState = _selectedState;
        _curvesEventsView.selectedState = _selectedState;
        _curvesFloatsView.selectedState = _selectedState;
        _eventView.Initialization();
        _curvesEventsView.Initialization();
    }

    void OnGUI()
    {
        _guiSplitWindow.StartToSplit();

        if (_selectedState != null)
        {
            if (gameObject != null && _editor == null) _editor = Editor.CreateEditor(gameObject);
            GUILayout.Space(10);

            if (_selectedState is IAnimatorCurves animatorCurves)
            {
                EditorGUILayout.BeginVertical("box");
                _curvesFloatsView.MainBody();
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
            }

            if (_selectedState is IEventCenterComponent)
            {
                if (_selectedState is ICurveEventsList curveEventsLists)
                {
                    EditorGUILayout.BeginVertical("box");
                    _curvesEventsView.MainBody();
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(10);
                }

                if (_selectedState is IEventsLists eventsLists)
                {
                    EditorGUILayout.BeginVertical("box");
                    _eventView.MainBody();
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(10);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Select correct Animation state", MessageType.Info);
        }

        _guiSplitWindow.Split();

        _playablePanel.PlayTimeLine();

        if (_modelPreview.target == null && gameObject != null) _modelPreview.target = gameObject;

        _modelPreview.OnPreview();

        EditorGUILayout.BeginHorizontal();
        gameObject = (GameObject)EditorGUILayout.ObjectField(gameObject, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();

        _guiSplitWindow.EndSplit();

        Repaint();
    }
}
