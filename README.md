# Unity-UIToolkit-Snapping-Draggable-ScrollView
A simple draft Unity UIToolkit ScrollView that provide drag and snapping function.
![Imgur](https://imgur.com/LXTHvgU.jpg)

## Usage
Pass ScrollView that need Snapping and Dragging to constructor as parameter.
> private DraggableScrollView _draggableScrollView;\
> _draggableScrollView = new DraggableScrollView(_scrollView, true, true);

## WIP features.
* Support use scroller to scroll with snapping.
* Support drag ScrollViewMode.Vertical and  VerticalAndHorizontal.

Use [Dotween](https://github.com/Demigiant/dotween) for snapping animation.
