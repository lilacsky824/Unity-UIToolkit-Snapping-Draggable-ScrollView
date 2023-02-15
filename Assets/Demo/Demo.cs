using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Demo : MonoBehaviour
{
    [SerializeField]
    private UIDocument _demoUI;
    private ScrollView _scrollView;
    private DraggableScrollView _draggableScrollView;

    void OnEnable()
    {
        _scrollView = _demoUI.rootVisualElement.Q<ScrollView>();
        _draggableScrollView = new DraggableScrollView(_scrollView, true, true);
        Button previous = _demoUI.rootVisualElement.Q<Button>("Previous");
        Button next = _demoUI.rootVisualElement.Q<Button>("Next");

        previous.RegisterCallback<ClickEvent>((e) => _draggableScrollView.ScrollToPreviosElement());
        next.RegisterCallback<ClickEvent>((e) => _draggableScrollView.ScrollToNextElement());

        List<Button> childrenButtons = _scrollView.contentContainer.Query<Button>().ToList();
        foreach(Button btn in childrenButtons)
        {
            btn.clicked += () => Debug.Log("Button clicked!");
        }
    }
}
