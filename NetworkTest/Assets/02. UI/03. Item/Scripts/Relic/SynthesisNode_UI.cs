using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SynthesisNode_UI : MonoBehaviour, IPointerClickHandler
{
    #region Serialized Fields
    [Header("Components")]
    public Image iconImage;
    #endregion

    #region Private Fields
    public InventoryItem LinkedItem { get; private set; }
    private RelicSynthesisManager manager;
    #endregion

    #region Public Methods
    public void Initialize(RelicSynthesisManager managerRef, InventoryItem item)
    {
        this.manager = managerRef;
        this.LinkedItem = item;

        if (iconImage != null)
            iconImage.sprite = Resources.Load<Sprite>(item.item.iconPath);
    }
    #endregion

    #region Event Handlers
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right ||
           (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2))
        {
            if (manager != null) manager.RemoveNode(this);
        }
    }
    #endregion
}