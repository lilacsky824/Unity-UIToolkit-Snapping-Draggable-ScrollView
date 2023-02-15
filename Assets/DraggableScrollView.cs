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
    private Dictionary<Button, PickingMode> _elementsInitialPickingMode;
    private List<VisualElement> _elements;
    private Rect[] _elementsRects;
    private bool _snapping = false;
    private float _snapDuration = 1.0f;

    private Tweener _snapTween;
    private float _scrollStart;
    private float _scrollTarget;

    public DraggableScrollView(ScrollView scrollView, bool snapping = true, bool canDragChildrenButtons = false)
    {
        _scrollView = scrollView;
        _scrollViewViewport = _scrollView.contentContainer.hierarchy.parent;
        _snapping = snapping;

        _scrollViewViewport.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        _scrollViewViewport.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        _scrollViewViewport.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        _scrollViewViewport.RegisterCallback<ClickEvent>(OnScrollViewClick);

        _scrollView.schedule.Execute(() => RefreshChildElementAndRects(true)).StartingIn(100);
    }

    void RefreshChildElementAndRects(bool canDragChildrenButtons = false)
    {
        _elements = _scrollView.contentContainer.Children().ToList();
        _elementsRects = new Rect[_elements.Count];
        for (int i = 0; i < _elements.Count; i++)
        {
            _elementsRects[i] = _elements[i].layout;
        }

        if (canDragChildrenButtons)
        {
            List<Button> childrenButtons = _scrollView.contentContainer.Query<Button>().ToList();
            _elementsInitialPickingMode = new Dictionary<Button, PickingMode>();

            foreach (Button b in childrenButtons)
            {
                b.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
                b.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
                b.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
                _elementsInitialPickingMode.Add(b, b.pickingMode);
            }
        }
    }

    public void SetSnappingDuration(float duration)
    {
        _snapDuration = duration;
    }

    public void ScrollToElement(VisualElement element)
    {

        bool forward = GetElementCenterValue(element) > GetScrollViewCenterValue();

        ScrollToTargetValue(GetElementCenterValue(element) - 0.5f * _scrollViewViewport.layout.width, forward);

    }

    public void ScrollToPreviosElement()
    {
        int previousIndex = GetNearestElementIndex() - 1;

        if (previousIndex < 0)
        {
            previousIndex = _elements.Count - 1;
        }

        ScrollToElement(_elements[previousIndex]);
    }

    public void ScrollToNextElement()
    {
        int nextIndex = GetNearestElementIndex() + 1;

        if (nextIndex >= _elements.Count)
        {
            nextIndex = 0;
        }

        ScrollToElement(_elements[nextIndex]);
    }

    public void ScrollToElementAtIndex(int index)
    {
        ScrollToElement(_elements[index]);
    }

    /// <summary>
    /// Get nearest element that closet to ScrollView center.
    /// </summary>

    int GetNearestElementIndex()
    {
        int nearest = 0;
        float previousDistance = 0;

        for (int i = 0; i < _elements.Count; i++)
        {
            VisualElement currentElement = _elements[i];
            float distance = Mathf.Abs(GetScrollViewCenterValue() - GetElementCenterValue(_elements[i]));

            if (i == 0)
                previousDistance = distance;

            if (distance < previousDistance)
            {
                previousDistance = distance;
                nearest = i;
            }
        }

        return nearest;
    }

    float GetElementCenterValue(VisualElement element)
    {
        return element.layout.center.x;
    }

    float GetScrollViewCenterValue()
    {
        return _scrollView.horizontalScroller.value + _scrollViewViewport.layout.width * 0.5f;
    }

    void ScrollToTargetValue(float target, bool forward)
    {
        if (_snapTween != null)
        {
            _snapTween.Kill();
        }

        _scrollTarget = target;
        _scrollStart = _scrollView.horizontalScroller.value;
        position = 0;

        _snapTween = DOVirtual.Float(_scrollStart, target, _snapDuration, v => _scrollView.horizontalScroller.value = v);
        _snapTween.onUpdate += (() => CheckCheckElementOutsideWhenTween(forward));
    }

    private float position;
    void CheckCheckElementOutsideWhenTween(bool placeForward)
    {
        bool isOutside = CheckElementOutside(placeForward, out float offset);

        if (isOutside)
        {
            _scrollTarget += offset;
            _scrollStart += offset;

            _snapTween.ChangeValues(_scrollStart, _scrollTarget);
            _snapTween.Goto(position, true);
        }
        else
        {
            position = _snapTween.position;
        }
    }

    /// <returns>Is outside?</returns>
    bool CheckElementOutside(bool placeForward, out float offset)
    {
        bool isInside = true;

        if (placeForward)
        {
            VisualElement element = _elements.First();
            isInside = _scrollView.worldBound.Overlaps(element.worldBound);
            if (!isInside)
            {
                element.BringToFront();

                _elements.Remove(element);
                _elements.Add(element);

                offset = -(element.worldBound.width + element.resolvedStyle.marginRight + element.resolvedStyle.marginLeft);
                _scrollView.horizontalScroller.value += offset;

                return true;
            }
        }
        else
        {
            VisualElement element = _elements.Last();
            isInside = _scrollView.worldBound.Overlaps(element.worldBound);
            if (!isInside)
            {
                element.SendToBack();
                _elements.Remove(element);
                _elements.Insert(0, element);
                offset = element.worldBound.width + element.resolvedStyle.marginLeft + element.resolvedStyle.marginRight;
                _scrollView.horizontalScroller.value += offset;
                return true;
            }
        }
        offset = 0;
        return false;
    }

    #region Drag
    //Thanks martinpa_unity https://forum.unity.com/threads/how-to-register-drag-and-click-events-on-the-same-visualelement.1189135/

    Vector2 _initialPosition;
    bool _dragged = false;
    bool _dragging = false;

    void OnPointerDown(PointerDownEvent evt)
    {
        if (_snapTween != null)
        {
            _snapTween.Kill();
        }
        
        _initialPosition = evt.position;
        _dragging = true;

        evt.StopPropagation();
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_dragging)
            return;

        // If the pointer moved at least 4-ish pixels in any direction at any point, we consider it a drag and drop operation.
        if ((_initialPosition - (Vector2)evt.position).sqrMagnitude > 16.0f && !_dragged)
        {
            _dragged = true;

            foreach (KeyValuePair<Button, PickingMode> pair in _elementsInitialPickingMode)
            {
                pair.Key.pickingMode = PickingMode.Ignore;
            }
        }

        _scrollView.horizontalScroller.value -= evt.deltaPosition.x;

        bool forward = evt.deltaPosition.x < 0;

        CheckElementOutside(forward, out _);

        evt.StopPropagation();
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        _dragging = false;

        if (_snapping)
            ScrollToElement(_elements[GetNearestElementIndex()]);
    }

    void OnScrollViewClick(ClickEvent e)
    {     
        if (_dragged)
        {
            foreach (KeyValuePair<Button, PickingMode> pair in _elementsInitialPickingMode)
            {
                pair.Key.pickingMode = pair.Value;
            }

            _dragged = false;
        }
    }
    #endregion
}