using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class DynamicButton : Button
{
    [SerializeField] private TMP_Text tmp_text;
    [SerializeField] private string text;
    [SerializeField] private Graphic[] additionalGraphics;

    protected override void Awake()
    {
        base.Awake();
        if (tmp_text)
            tmp_text.text = text;
    }

    protected Graphic[] Graphics
    {
        get
        {
            if (additionalGraphics == null) // 캐싱이 되지 않았다면
                additionalGraphics = targetGraphic.transform.GetComponentsInChildren<Graphic>(); // 자식 오브젝트로부터 Graphic 컴포넌트 지정
            return additionalGraphics;
        }
    }
    [SerializeField] private Color additionalNormalColor = Color.white;
    [SerializeField] private Color additionalHighlightedColor = Color.white;
    [SerializeField] private Color additionalPressedColor = Color.white;
    [SerializeField] private Color additionalSelectedColor = Color.white;
    [SerializeField] private Color additionalDisabledColor = Color.white;

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        Color color, additional;
        switch (state)
        {
            case SelectionState.Normal:
                color = colors.normalColor;
                additional = additionalNormalColor;
                break;
            case SelectionState.Highlighted:
                color = colors.highlightedColor;
                additional = additionalHighlightedColor;
                break;
            case SelectionState.Pressed:
                color = colors.pressedColor;
                additional = additionalPressedColor;
                break;
            case SelectionState.Selected:
                color = colors.selectedColor;
                additional = additionalSelectedColor;
                break;
            case SelectionState.Disabled:
                color = colors.disabledColor;
                additional = additionalDisabledColor;
                break;
            default:
                color = Color.black;
                additional = Color.black;
                break;
        }

        if (gameObject.activeInHierarchy)
        {
            switch (transition)
            {
                case Transition.ColorTint: //Color Tint
                    ColorTween(color * colors.colorMultiplier, additional * colors.colorMultiplier, instant);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    private void ColorTween(Color targetColor, Color additionalColor, bool instant)
    {
        if (targetGraphic == null)
            return;
        targetGraphic.CrossFadeColor(targetColor, (!instant) ? colors.fadeDuration : 0f, true, true);
        for (int i = 0; i < Graphics.Length; ++i)
            Graphics[i].CrossFadeColor(additionalColor, (!instant) ? colors.fadeDuration : 0f, true, true);
    }
}
