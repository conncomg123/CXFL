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

        public VanillaFrame()
        {
            Background = Colors.DarkGray;
            BorderColor = Colors.Grey;
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

            var blackBox = new BoxView { BackgroundColor = Colors.Black };
            grid.Children.Add(blackBox);
            Grid.SetRow(blackBox, 0);
            Grid.SetColumnSpan(blackBox, 2);

            Content = grid;

        }
        private void VanillaFrame_PointerEntered(object sender, PointerEventArgs e)
        {
            //Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNorthwestSoutheast, 1);
        }

        private void VanillaFrame_PointerExited(object sender, PointerEventArgs e)
        {
            if (!isResizing)
            {
                //Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
            }
        }

        private void VanillaFrame_PointerPressed(object sender, PointerEventArgs e)
        {
            if (IsPointerOverBorder(e))
            {
                isResizing = true;
                resizeStartPosition = e.GetPosition(this);
            }
        }

        private void VanillaFrame_PointerReleased(object sender, PointerEventArgs e)
        {
            isResizing = false;
        }

        private void VanillaFrame_PointerMoved(object sender, PointerEventArgs e)
        {
            if (isResizing)
            {
                var currentPosition = e.GetPosition(this);
                var deltaX = currentPosition?.X - resizeStartPosition?.X;
                var deltaY = currentPosition?.Y - resizeStartPosition?.Y;

                WidthRequest = (double)(InitialWidth + deltaX);
                HeightRequest = (double)(InitialHeight + deltaY);
            }
        }

        private bool IsPointerOverBorder(PointerEventArgs e)
        {
            var pointerPosition = e.GetPosition(this);
            return pointerPosition?.X < 5 || pointerPosition?.Y < 5 || pointerPosition?.X > Width - 5 || pointerPosition?.Y > Height - 5;
        }
    }
}
