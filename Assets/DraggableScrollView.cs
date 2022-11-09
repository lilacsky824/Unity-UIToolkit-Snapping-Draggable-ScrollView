using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Draggable ScrollView with snapping function.
/// </summary>
[System.Serializable]
public class DraggableScrollView
{
    private ScrollView _scrollView;
    private VisualElement _scrollViewViewport;
    private VisualElement[] _elements;
    private Dictionary<Button, bool> _elementsInitialEnabled;
    private Rect[] _elementsRects;
    private bool _snapping = false;
    private float _snapDuration = 1.0f;

    public DraggableScrollView(ScrollView scrollView, bool snapping = true, bool canDragChildrenButtons = false)
    {
        _scrollView = scrollView;
        _scrollViewViewport = _scrollView.contentContainer.hierarchy.parent;
        _snapping = snapping;
        _elementsInitialEnabled = new Dictionary<Button, bool>();

        _scrollViewViewport.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        _scrollViewViewport.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        _scrollViewViewport.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);

        _scrollView.schedule.Execute(() => RefreshChildElementAndRects(true)).StartingIn(100);
    }

    void RefreshChildElementAndRects(bool canDragChildrenButtons = false)
    {
        _elements = _scrollView.contentContainer.Children().ToArray<VisualElement>();
        _elementsRects = new Rect[_elements.Length];
        for (int i = 0; i < _elements.Length; i++)
        {
            _elementsRects[i] = _elements[i].layout;
        }

        if (canDragChildrenButtons)
        {
            List<Button> childrenButtons = _scrollView.contentContainer.Query<Button>().ToList();

            foreach (Button b in childrenButtons)
            {
                b.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
                b.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
                b.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
                _elementsInitialEnabled.Add(b, b.enabledSelf);
            }
        }
    }

    public void SetSnappingDuration(float duration)
    {
        _snapDuration = duration;
    }

    public void ScrollToElement(VisualElement element)
    {
        ScrollToTargetValue(GetElementCenterValue(element) - 0.5f * _scrollViewViewport.layout.width);
    }

    public void ScrollToPreviosElement()
    {
        int previousIndex = GetNearestElementIndex() - 1;

        if (previousIndex < 0)
            return;

        ScrollToElement(_elements[previousIndex]);
    }

    public void ScrollToNextElement()
    {
        int nextIndex = GetNearestElementIndex() + 1;

        if (nextIndex >= _elements.Length)
            return;

        ScrollToElement(_elements[nextIndex]);
    }

    public void ScrollToElementAtIndex(int index)
    {
        ScrollToElement(_elements[index]);
    }

    /// <summary>
    /// Get nearest element index that closet to ScrollView center.
    /// </summary>

    int GetNearestElementIndex()
    {
        int nearestIndex = 0;

        float previousDistance = 0;

        for (int i = 0; i < _elements.Length; i++)
        {
            float distance = Mathf.Abs(GetScrollViewCenterValue() - GetElementCenterValue(_elements[i]));

            if (i == 0)
                previousDistance = distance;

            if (distance < previousDistance)
            {
                previousDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    float GetElementCenterValue(int index)
    {
        return GetElementCenterValue(_elements[index]);
    }

    float GetElementCenterValue(VisualElement element)
    {
        return element.layout.center.x;
    }

    float GetScrollViewCenterValue()
    {
        return _scrollView.horizontalScroller.value + _scrollViewViewport.layout.width * 0.5f;
    }

    void ScrollToTargetValue(float target)
    {
        Sequence s = DOTween.Sequence();
        s.Append(DOVirtual.Float(_scrollView.horizontalScroller.value, target, _snapDuration, v => _scrollView.horizontalScroller.value = v)).SetEase(Ease.InOutQuad);
    }

    #region Drag
    //Thanks martinpa_unity https://forum.unity.com/threads/how-to-register-drag-and-click-events-on-the-same-visualelement.1189135/

    Vector2 _initialPosition;
    bool _dragged = false;
    bool _dragging = false;
    bool _canClick = true;
    void OnPointerDown(PointerDownEvent evt)
    {
        _initialPosition = evt.position;
        _dragging = true;
        _dragged = false;

        evt.StopPropagation();
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_dragging)
            return;

        // If the pointer moved at least 4-ish pixels in any direction at any point, we consider it a drag and drop operation.
        if ((_initialPosition - (Vector2)evt.position).sqrMagnitude > 16.0f)
            _dragged = true;

        _scrollView.horizontalScroller.value -= evt.deltaPosition.x;
        evt.StopPropagation();
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        _dragging = false;

        // If target is a button, stop interation temporarily to prevent trigger click event when release.
        if (_dragged && evt.currentTarget is Button)
        {
            Button element = evt.currentTarget as Button;
            if (_elementsInitialEnabled[element])
            {
                element.SetEnabled(false);
                element.schedule.Execute(() => element.SetEnabled(true));
            }
        }

        if (_snapping)
            ScrollToElement(_elements[GetNearestElementIndex()]);
    }
    #endregion
}