using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

public class JoystickInputAreaUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler {
    [SerializeField] private OnScreenStick onScreenStickScript;
    [SerializeField] private RectTransform floatingJoystickRectTransform;
    
    private RectTransform _joystickInputAreaRectTransform;
    private Vector2 _joystickOriginalPosition;
    
    private void Start() {
        _joystickInputAreaRectTransform = GetComponent<RectTransform>();
        _joystickOriginalPosition = floatingJoystickRectTransform.anchoredPosition;
    }
    
    public void OnPointerDown(PointerEventData eventData) {
        Debug.Log("OnPointerDown");
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _joystickInputAreaRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint)) {
            Debug.Log("OnPointerDown In Rectangle, localPoint: " + localPoint);

            floatingJoystickRectTransform.anchoredPosition = localPoint;
            // stickInputAreaRectTransform.gameObject.SetActive(false);
            onScreenStickScript.OnPointerDown(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData) {
        Debug.Log("OnPointerUp");
        onScreenStickScript.OnPointerUp(eventData);
        // stickInputAreaRectTransform.gameObject.SetActive(true);
        floatingJoystickRectTransform.anchoredPosition = _joystickOriginalPosition;
    }

    public void OnDrag(PointerEventData eventData) {
        onScreenStickScript.OnDrag(eventData);
    }
}
