using CommunityToolkit.Maui.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CXFLGUI
{
    public class VanillaFrame : Frame
    {
        private bool isResizing = false;
        private Microsoft.Maui.Graphics.Point? resizeStartPosition;

        double InitialWidth = 300;
        double InitialHeight = 250;

        int ResizeTolerance = 15;

        public VanillaFrame()
        {
            Background = (Color)App.Fixed_ResourceDictionary["Colors"]["Primary"];
            BorderColor = (Color)App.Fixed_ResourceDictionary["Colors"]["Gray500"];
            CornerRadius = 5;
            HasShadow = true;
            Padding = 1;
            HeightRequest = InitialHeight;
            WidthRequest = InitialWidth;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25, GridUnitType.Absolute) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var topBrush = new BoxView { Color = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryDark"] };
            grid.Children.Add(topBrush);
            Grid.SetRow(topBrush, 0);
            Grid.SetColumnSpan(topBrush, 2);

            Content = grid;

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            this.GestureRecognizers.Add(panGesture);

        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    if (IsPointerOverBorder(e.TotalX, e.TotalY))
                    {
                        isResizing = true;
                        resizeStartPosition = new Point(e.TotalX, e.TotalY);
                    }
                    break;
                case GestureStatus.Running:
                    if (isResizing && resizeStartPosition != null)
                    {
                        var deltaX = e.TotalX - resizeStartPosition.Value.X;
                        var deltaY = e.TotalY - resizeStartPosition.Value.Y;

                        WidthRequest = InitialWidth + deltaX;
                        HeightRequest = InitialHeight + deltaY;
                    }
                    break;
                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    isResizing = false;
                    break;
            }
        }

        private void VanillaFrame_PointerEntered(object sender, PointerEventArgs e)
        {
            if (sender is VisualElement visualElement)
            {
                CursorBehavior.SetCursor(visualElement, CursorIcon.Cross);
            }
        }

        private void VanillaFrame_PointerExited(object sender, PointerEventArgs e)
        {
            if (sender is VisualElement visualElement)
            {
                CursorBehavior.SetCursor(visualElement, CursorIcon.Hand);
            }
        }

        private void VanillaFrame_PointerPressed(object sender, PointerEventArgs e)
        {
            var position = e.GetPosition(this);
            if (IsPointerOverBorder((double)(position?.X), (double)(position?.Y)))
            {
                isResizing = true;
                resizeStartPosition = position;
            }
        }

        private void VanillaFrame_PointerReleased(object sender, PointerEventArgs e)
        {
            isResizing = false;
        }

        private void VanillaFrame_PointerMoved(object sender, PointerEventArgs e)
        {
            if (isResizing && resizeStartPosition != null)
            {
                var currentPosition = e.GetPosition(this);
                var deltaX = currentPosition?.X - resizeStartPosition?.X;
                var deltaY = currentPosition?.Y - resizeStartPosition?.Y;

                WidthRequest = (double)(InitialWidth + deltaX);
                HeightRequest = (double)(InitialHeight + deltaY);
            }
        }

        private bool IsPointerOverBorder(double x, double y)
        {
            return x < ResizeTolerance || y < ResizeTolerance || x > Width - ResizeTolerance || y > Height - ResizeTolerance;
        }
    }
}
