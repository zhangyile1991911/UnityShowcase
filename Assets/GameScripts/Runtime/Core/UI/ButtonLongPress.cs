using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ButtonLongPress : Button, IPointerDownHandler, IPointerUpHandler
{
    public float longPressDuration = 1.0f;
    
    [FormerlySerializedAs("onLongPress")]
    [SerializeField]
    private ButtonClickedEvent m_OnLongPress = new ButtonClickedEvent();
    
    [FormerlySerializedAs("onLongPressEnd")]
    [SerializeField]
    private ButtonClickedEvent m_OnLongPressEnd = new ButtonClickedEvent();

    public ButtonClickedEvent onLongPress
    {
        get { return m_OnLongPress; }
        set { m_OnLongPress = value; }
    }
    
    public ButtonClickedEvent onLongPressEnd
    {
        get { return m_OnLongPressEnd; }
        set { m_OnLongPressEnd = value; }
    }
    // public UnityEvent onLongPress;
    // public UnityEvent onLongPressEnd;

    private bool isPressed = false;
    public float pressTime = 0.1f;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsActive() || !IsInteractable())
            return;
        isPressed = true;
        pressTime = Time.time;
        InvokeRepeating("OnLongPress", 0.0f, 0.1f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsActive() || !IsInteractable())
            return;
        
        isPressed = false;
        CancelInvoke("OnLongPress");
        onLongPressEnd.Invoke();
    }

    private void OnLongPress()
    {
        if (!IsActive() || !IsInteractable())
            return;
        
        if (isPressed && Time.time - pressTime >= longPressDuration)
        {
            onLongPress.Invoke();
        }
    }
}
