using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SynthesisNode_UI : MonoBehaviour, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Floating Effect")]
    public float floatSpeed = 15f;
    public float floatRange = 15f;

    [Header("Components")]
    public Image iconImage;
    public Button removeButton;

    public InventoryItem LinkedItem { get; private set; }

    private RelicSynthesisManager manager; 
    private Vector3 floatBasePos;
    private Vector3 randomOffset;
    private bool isDragging = false;

    public void Initialize(RelicSynthesisManager managerRef, InventoryItem item)
    {
        this.manager = managerRef;
        this.LinkedItem = item;

        // 이미지 설정
        if (iconImage != null)
            iconImage.sprite = Resources.Load<Sprite>(item.item.iconPath);

        // 무중력 초기값 설정
        floatBasePos = transform.localPosition; // 로컬 좌표 기준
        randomOffset = new Vector3(Random.Range(0, 100), Random.Range(0, 100), 0);
    }

    void Start()
    {
        if (removeButton != null) removeButton.onClick.AddListener(OnRemoveClick);
    }

    void Update()
    {
        // 드래그 중이 아닐 때만 둥둥 떠다님
        if (!isDragging)
        {
            float time = Time.time * floatSpeed * 0.01f;
            float x = (Mathf.PerlinNoise(time + randomOffset.x, 0) - 0.5f) * floatRange;
            float y = (Mathf.PerlinNoise(0, time + randomOffset.y) - 0.5f) * floatRange;

            // 로컬 좌표 기준으로 이동
            transform.localPosition = Vector3.Lerp(transform.localPosition, floatBasePos + new Vector3(x, y, 0), Time.deltaTime * 5f);
        }
    }

    public void OnRemoveClick()
    {
        if (manager != null) manager.RemoveNode(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        floatBasePos = transform.localPosition; 
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 필요 시 툴팁 표시
    }
}