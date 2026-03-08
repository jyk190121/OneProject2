using System;
using UnityEngine;
using UnityEngine.UIElements;
public abstract class BasePopup : MonoBehaviour
{
    protected UIDocument popupDocument;
    protected VisualElement root;
    protected Action onClosed;

    protected virtual void Awake()
    {
        popupDocument = GetComponent<UIDocument>();
        root = popupDocument.rootVisualElement;

        root.style.display = DisplayStyle.Flex;
    }

    public virtual void Show(Action onCloseCallback)
    {
        onClosed = onCloseCallback;
        // DocumentUI 숨김
        root.style.display = DisplayStyle.None;
    }

    public virtual void Close()
    {
        // DocumentUI 보여주기
        root.style.display = DisplayStyle.Flex;
        onClosed?.Invoke();
    }
}