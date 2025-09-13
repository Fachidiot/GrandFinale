using UnityEditor;
using UnityEngine;

public class ModelPreview
{
    private Editor _targetEditor;
    public GameObject target;
    public AnimationClip animationClip;
    private float _animationTime;

    public void AnimationTimeUpdate(float time)
    {
        if (animationClip != null) _animationTime = animationClip.length;
        _animationTime *= time;
    }

    public void OnPreview()
    {
        if (EditorApplication.isPlaying) return;
        if (target == null) return;
        if (_targetEditor == null) _targetEditor = Editor.CreateEditor(target);
        Rect previewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
        _targetEditor.OnInteractivePreviewGUI(previewRect, new GUIStyle());

        if (animationClip != null)
        {
            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
            }
        }
        else if (AnimationMode.InAnimationMode())
            AnimationMode.StopAnimationMode();

        if (!EditorApplication.isPlaying && AnimationMode.InAnimationMode() && animationClip != null)
        {
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip((GameObject)_targetEditor.target, animationClip, _animationTime);
            _targetEditor.ReloadPreviewInstances();
            AnimationMode.EndSampling();
        }
    }
}