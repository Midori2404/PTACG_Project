using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropdownMouseWheel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ScrollRect scrollRect;
    private bool isPointerOver = false;

    void Start()
    {
        scrollRect = GetComponentInChildren<ScrollRect>(); // Get ScrollRect inside dropdown
    }

    void Update()
    {
        if (isPointerOver && scrollRect != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            scrollRect.verticalNormalizedPosition += scroll * 2.0f; // Adjust speed if needed
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
    }
}
