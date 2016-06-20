using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Precog.Custom_Controls
{
    public class ScrollableTabPanel : Panel, IScrollInfo, INotifyPropertyChanged
    {

        private ScrollViewer _svOwningScrollViewer;
        private bool _fCanScroll_H = true;
        private Size _szControlExtent = new Size(0, 0);
        private Size _szViewport = new Size(0, 0);
        private Vector _vOffset;

        private static GradientStopCollection _gscOpacityMaskStops_TransparentOnLeftAndRight = new GradientStopCollection{
            new GradientStop(Colors.Transparent,0.0),
            new GradientStop(Colors.Black, 0.2),
            new GradientStop(Colors.Black, 0.8),
            new GradientStop(Colors.Transparent,1.0)
         };
        private static GradientStopCollection _gscOpacityMaskStops_TransparentOnLeft = new GradientStopCollection{
            new GradientStop(Colors.Transparent,0),
            new GradientStop(Colors.Black, 0.5)
         };
        private static GradientStopCollection _gscOpacityMaskStops_TransparentOnRight = new GradientStopCollection{
            new GradientStop(Colors.Black, 0.5),
            new GradientStop(Colors.Transparent, 1)
         };

        private TranslateTransform _ttScrollTransform = new TranslateTransform();



        public ScrollableTabPanel()
        {
            this.RenderTransform = _ttScrollTransform;
            this.SizeChanged += new SizeChangedEventHandler(ScrollableTabPanel_SizeChanged);
        }

        private static double CalculateNewScrollOffset(
              double dblViewport_Left,
              double dblViewport_Right,
              double dblChild_Left,
              double dblChild_Right
           )
        {
            bool fIsFurtherToLeft = (dblChild_Left < dblViewport_Left) && (dblChild_Right < dblViewport_Right);
            bool fIsFurtherToRight = (dblChild_Right > dblViewport_Right) && (dblChild_Left > dblViewport_Left);
            bool fIsWiderThanViewport = (dblChild_Right - dblChild_Left) > (dblViewport_Right - dblViewport_Left);

            if (!fIsFurtherToRight && !fIsFurtherToLeft)
                return dblViewport_Left;

            if (fIsFurtherToLeft && !fIsWiderThanViewport)
                return dblChild_Left;

            return (dblChild_Right - (dblViewport_Right - dblViewport_Left));
        }

        private void UpdateMembers(Size szExtent, Size szViewportSize)
        {
            if (szExtent != this.Extent)
            {
                this.Extent = szExtent;
                if (this.ScrollOwner != null) this.ScrollOwner.InvalidateScrollInfo();
            }

            if (szViewportSize != this.Viewport)
            {
                this.Viewport = szViewportSize;
                if (this.ScrollOwner != null)
                    this.ScrollOwner.InvalidateScrollInfo();
            }

            if (this.HorizontalOffset + this.Viewport.Width + this.RightOverflowMargin > this.ExtentWidth)
                SetHorizontalOffset(HorizontalOffset + this.Viewport.Width + this.RightOverflowMargin);

            NotifyPropertyChanged("CanScroll");
            NotifyPropertyChanged("CanScrollLeft");
            NotifyPropertyChanged("CanScrollRight");
        }

        private double getLeftEdge(UIElement uieChild)
        {
            double dblWidth = 0;
            double dblWidth_Total = 0;

            foreach (UIElement uie in this.InternalChildren)
            {
                dblWidth = uie.DesiredSize.Width;

                if (uieChild != null && uieChild == uie)
                    return dblWidth_Total;

                dblWidth_Total += dblWidth;
            }

            return dblWidth_Total;
        }

        public bool IsPartlyVisible(UIElement uieChild)
        {
            Rect rctIntersect = GetIntersectionRectangle(uieChild);
            return (!(rctIntersect == Rect.Empty));
        }

        public double PartlyVisiblePortion_OverflowToRight(UIElement uieChild)
        {
            Rect rctIntersect = GetIntersectionRectangle(uieChild);
            double dblVisiblePortion = 1;
            if (
                  !(rctIntersect == Rect.Empty)
                  &&
                  this.CanScrollRight
                  &&
                  rctIntersect.Width < uieChild.DesiredSize.Width
                  &&
                  rctIntersect.X > 0
               )
                dblVisiblePortion = rctIntersect.Width / uieChild.DesiredSize.Width;

            return dblVisiblePortion;
        }

        public double PartlyVisiblePortion_OverflowToLeft(UIElement uieChild)
        {
            Rect rctIntersect = GetIntersectionRectangle(uieChild);
            double dblVisiblePortion = 1;
            if (
                  !(rctIntersect == Rect.Empty)
                  &&
                  this.CanScrollLeft
                  &&
                  rctIntersect.Width < uieChild.DesiredSize.Width
                  &&
                  rctIntersect.X == 0
               )
                dblVisiblePortion = rctIntersect.Width / uieChild.DesiredSize.Width;

            return dblVisiblePortion;
        }

        private Rect GetScrollViewerRectangle()
        {
            return new Rect(new Point(0, 0), this.ScrollOwner.RenderSize);
        }

        private Rect GetChildRectangle(UIElement uieChild)
        {
            GeneralTransform childTransform = uieChild.TransformToAncestor(this.ScrollOwner);
            return childTransform.TransformBounds(new Rect(new Point(0, 0), uieChild.RenderSize));
        }

        private Rect GetIntersectionRectangle(UIElement uieChild)
        {
            Rect rctScrollViewerRectangle = GetScrollViewerRectangle();
            Rect rctChildRect = GetChildRectangle(uieChild);

            return Rect.Intersect(rctScrollViewerRectangle, rctChildRect);
        }

        private void RemoveOpacityMasks()
        {
            foreach (UIElement uieChild in Children)
            {
                RemoveOpacityMask(uieChild);
            }
        }

        private void RemoveOpacityMask(UIElement uieChild)
        {
            uieChild.OpacityMask = null;
        }

        private void UpdateOpacityMasks()
        {
            foreach (UIElement uieChild in Children)
            {
                UpdateOpacityMask(uieChild);
            }
        }
        private void UpdateOpacityMask(UIElement uieChild)
        {
            if (uieChild == null) return;

            Rect rctScrollViewerRectangle = GetScrollViewerRectangle();
            if (rctScrollViewerRectangle == Rect.Empty) return;

            Rect rctChildRect = GetChildRectangle(uieChild);

            if (rctScrollViewerRectangle.Contains(rctChildRect))
                uieChild.OpacityMask = null;
            else
            {
                double dblPartlyVisiblePortion_OverflowToLeft = PartlyVisiblePortion_OverflowToLeft(uieChild);
                double dblPartlyVisiblePortion_OverflowToRight = PartlyVisiblePortion_OverflowToRight(uieChild);

                if (dblPartlyVisiblePortion_OverflowToLeft < 1 && dblPartlyVisiblePortion_OverflowToRight < 1)
                    uieChild.OpacityMask = new LinearGradientBrush(
                          _gscOpacityMaskStops_TransparentOnLeftAndRight,
                          new Point(0, 0),
                          new Point(1, 0));
                else if (dblPartlyVisiblePortion_OverflowToLeft < 1)
                    uieChild.OpacityMask = new LinearGradientBrush(
                          _gscOpacityMaskStops_TransparentOnLeft,
                          new Point(1 - dblPartlyVisiblePortion_OverflowToLeft, 0),
                          new Point(1, 0)
                       );
                else if (dblPartlyVisiblePortion_OverflowToRight < 1)
                    uieChild.OpacityMask = new LinearGradientBrush(
                          _gscOpacityMaskStops_TransparentOnRight,
                          new Point(0, 0),
                          new Point(dblPartlyVisiblePortion_OverflowToRight, 0)
                       );
                else
                    uieChild.OpacityMask = null;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size resultSize = new Size(0, availableSize.Height);

            foreach (UIElement uieChild in this.InternalChildren)
            {
                uieChild.Measure(availableSize);
                resultSize.Width += uieChild.DesiredSize.Width;
            }

            UpdateMembers(resultSize, availableSize);

            double dblNewWidth = double.IsPositiveInfinity(availableSize.Width) ?
                resultSize.Width : availableSize.Width;

            resultSize.Width = dblNewWidth;
            return resultSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.InternalChildren == null || this.InternalChildren.Count < 1)
                return finalSize;

            double dblWidth = 0;
            double dblWidth_Total = 0;
            foreach (UIElement uieChild in this.InternalChildren)
            {
                dblWidth = uieChild.DesiredSize.Width;
                uieChild.Arrange(new Rect(dblWidth_Total, 0, dblWidth, uieChild.DesiredSize.Height));
                dblWidth_Total += dblWidth;
            }

            return finalSize;
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
            UpdateOpacityMasks();
        }

        protected override void OnChildDesiredSizeChanged(UIElement child)
        {
            base.OnChildDesiredSizeChanged(child);
            UpdateOpacityMasks();
        }


        public bool CanHorizontallyScroll
        {
            get { return _fCanScroll_H; }
            set { _fCanScroll_H = value; }
        }

        public bool CanVerticallyScroll
        {
            get { return false; }
            set { }
        }

        public double ExtentHeight
        {
            get { return this.Extent.Height; }
        }

        public double ExtentWidth
        {
            get { return this.Extent.Width; }
        }

        public double HorizontalOffset
        {
            get { return _vOffset.X; }
            private set { _vOffset.X = value; }
        }

        public void LineDown()
        {
            throw new InvalidOperationException();
        }

        public void LineLeft()
        {
            SetHorizontalOffset(this.HorizontalOffset - this.LineScrollPixelCount);
        }

        public void LineRight()
        {
            SetHorizontalOffset(this.HorizontalOffset + this.LineScrollPixelCount);
        }

        public void LineUp()
        {
            throw new InvalidOperationException();
        }

        public System.Windows.Rect MakeVisible(System.Windows.Media.Visual visual, System.Windows.Rect rectangle)
        {
            if (rectangle.IsEmpty || visual == null
              || visual == this || !base.IsAncestorOf(visual))
            { return Rect.Empty; }

            double dblOffsetX = 0;
            UIElement uieControlToMakeVisible = null;
            for (int i = 0; i < this.InternalChildren.Count; i++)
            {
                if ((Visual)this.InternalChildren[i] == visual)
                {
                    uieControlToMakeVisible = this.InternalChildren[i];
                    dblOffsetX = getLeftEdge(this.InternalChildren[i]);
                    break;
                }
            }

            if (uieControlToMakeVisible != null)
            {
                if (uieControlToMakeVisible == this.InternalChildren[0])
                    dblOffsetX = 0;
                else if (uieControlToMakeVisible == this.InternalChildren[this.InternalChildren.Count - 1])
                    dblOffsetX = this.ExtentWidth - this.Viewport.Width;
                else
                    dblOffsetX = CalculateNewScrollOffset(
                             this.HorizontalOffset,
                             this.HorizontalOffset + this.Viewport.Width,
                             dblOffsetX,
                             dblOffsetX + uieControlToMakeVisible.DesiredSize.Width
                       );

                SetHorizontalOffset(dblOffsetX);
                rectangle = new Rect(this.HorizontalOffset, 0, uieControlToMakeVisible.DesiredSize.Width, this.Viewport.Height);
            }

            return rectangle;
        }

        public void MouseWheelDown()
        {
        }

        public void MouseWheelLeft()
        {
        }

        public void MouseWheelRight()
        {
        }

        public void MouseWheelUp()
        {
        }

        public void PageDown()
        {
        }

        public void PageLeft()
        {
        }

        public void PageRight()
        {
        }

        public void PageUp()
        {
        }

        public ScrollViewer ScrollOwner
        {
            get { return _svOwningScrollViewer; }
            set
            {
                _svOwningScrollViewer = value;
                if (_svOwningScrollViewer != null)
                    this.ScrollOwner.Loaded += new RoutedEventHandler(ScrollOwner_Loaded);
                else
                    this.ScrollOwner.Loaded -= new RoutedEventHandler(ScrollOwner_Loaded);
            }
        }

        public void SetHorizontalOffset(double offset)
        {
            RemoveOpacityMasks();

            this.HorizontalOffset = Math.Max(0, Math.Min(this.ExtentWidth - this.Viewport.Width, Math.Max(0, offset)));

            if (this.ScrollOwner != null) this.ScrollOwner.InvalidateScrollInfo();

            DoubleAnimation daScrollAnimation =
               new DoubleAnimation(
                     _ttScrollTransform.X,
                     (-this.HorizontalOffset),
                     new Duration(this.AnimationTimeSpan),
                     FillBehavior.HoldEnd
                  );

            daScrollAnimation.AccelerationRatio = 0.5;
            daScrollAnimation.DecelerationRatio = 0.5;

            daScrollAnimation.Completed += new EventHandler(daScrollAnimation_Completed);

            _ttScrollTransform.BeginAnimation(
                  TranslateTransform.XProperty,
                  daScrollAnimation,
                  HandoffBehavior.Compose);

            InvalidateMeasure();
        }

        public void SetVerticalOffset(double offset)
        {
            throw new InvalidOperationException();
        }

        public double VerticalOffset
        {
            get { return 0; }
        }

        public double ViewportHeight
        {
            get { return this.Viewport.Height; }
        }

        public double ViewportWidth
        {
            get { return this.Viewport.Width; }
        }


        public Size Extent
        {
            get { return _szControlExtent; }
            private set { _szControlExtent = value; }
        }

        public Size Viewport
        {
            get { return _szViewport; }
            private set { _szViewport = value; }
        }


        public bool IsOnFarLeft { get { return this.HorizontalOffset == 0; } }

        public bool IsOnFarRight { get { return (this.HorizontalOffset + this.Viewport.Width) == this.ExtentWidth; } }

        public bool CanScroll { get { return this.ExtentWidth > this.Viewport.Width; } }

        public bool CanScrollLeft { get { return this.CanScroll && !this.IsOnFarLeft; } }

        public bool CanScrollRight { get { return this.CanScroll && !this.IsOnFarRight; } }



        public static readonly DependencyProperty RightOverflowMarginProperty =
           DependencyProperty.Register("RightOverflowMargin", typeof(int), typeof(ScrollableTabPanel),
           new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
        public int RightOverflowMargin
        {
            get { return (int)GetValue(RightOverflowMarginProperty); }
            set { SetValue(RightOverflowMarginProperty, value); }
        }

        public static readonly DependencyProperty AnimationTimeSpanProperty =
           DependencyProperty.Register("AnimationTimeSpanProperty", typeof(TimeSpan), typeof(ScrollableTabPanel),
           new FrameworkPropertyMetadata(new TimeSpan(0, 0, 0, 0, 100), FrameworkPropertyMetadataOptions.AffectsRender));
        public TimeSpan AnimationTimeSpan
        {
            get { return (TimeSpan)GetValue(AnimationTimeSpanProperty); }
            set { SetValue(AnimationTimeSpanProperty, value); }
        }

        public static readonly DependencyProperty LineScrollPixelCountProperty =
           DependencyProperty.Register("LineScrollPixelCount", typeof(int), typeof(ScrollableTabPanel),
           new FrameworkPropertyMetadata(15, FrameworkPropertyMetadataOptions.AffectsRender));
        public int LineScrollPixelCount
        {
            get { return (int)GetValue(LineScrollPixelCountProperty); }
            set { SetValue(LineScrollPixelCountProperty, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String strPropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(strPropertyName));
            }
        }

        void ScrollOwner_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateOpacityMasks();
        }
        void daScrollAnimation_Completed(object sender, EventArgs e)
        {
            UpdateOpacityMasks();

            foreach (UIElement uieChild in this.InternalChildren)
                uieChild.InvalidateArrange();
        }

        void ScrollableTabPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateOpacityMasks();
        }

    }
}
