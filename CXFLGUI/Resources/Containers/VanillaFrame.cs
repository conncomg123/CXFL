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

        // Patented Soundman warning:
        // If you get trapped in COMException hell, it is likely because you have made
        // two of the same control in your Main Page Xaml Cs to debug this object.
        // Don't do that! Right now we require unique tabs and content with unique
        // identifiers, or else you will get sent to the shadow realm.
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
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25, GridUnitType.Absolute) }); // Row for tabs
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star }); // Remaining space

            // Create a StackLayout for tabs
            var tabStackLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Spacing = 0 // No spacing between tabs
            };

            // Add the StackLayout for tabs to the grid
            grid.Children.Add(tabStackLayout);
            Grid.SetRow(tabStackLayout, 0);
            Grid.SetColumnSpan(tabStackLayout, 2);

            // Create a separate StackLayout for content views
            var contentStackLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            // Add the StackLayout for content to the grid
            grid.Children.Add(contentStackLayout);
            Grid.SetRow(contentStackLayout, 1);
            Grid.SetColumnSpan(contentStackLayout, 2);

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            this.GestureRecognizers.Add(panGesture);

            Content = grid;
        }

        // Tab-Content Pairing System
        public void AddContent(View view)
        {
            var grid = (Grid)Content;
            var contentStackLayout = (StackLayout)grid.Children[1]; // Access the content stack layout
            contentStackLayout.Children.Add(view);
        }

        public void AddTab(string tabName)
        {
            var grid = (Grid)Content;
            var tabStackLayout = (StackLayout)grid.Children[0]; // Access the tab stack layout
            var tabButton = new Button
            {
                Text = tabName,
            };

            tabStackLayout.Children.Add(tabButton);
        }

        public void AddTabContentPair(string tabName, View content)
        {
            var grid = (Grid)Content;
            var tabStackLayout = (StackLayout)grid.Children[0]; // Access the tab stack layout
            var contentStackLayout = (StackLayout)grid.Children[1]; // Access the content stack layout

            // Create the tab button
            var tabButton = new Button
            {
                Text = tabName,
            };
            // Assign the event handler
            tabButton.Clicked += (sender, e) => ShowContent(content);

            // Add the tab button to the tab stack
            tabStackLayout.Children.Add(tabButton);

            // Add the content initially hidden
            content.IsVisible = false;
            contentStackLayout.Children.Add(content);

            // Show the content of the first tab
            if (tabStackLayout.Children.Count == 1)
            {
                ShowContent(content);
            }
        }

        private void ShowContent(View content)
        {
            var grid = (Grid)Content;
            var contentStackLayout = (StackLayout)grid.Children[1]; // Access the content stack layout

            // Hide all content views except the selected one
            foreach (var child in contentStackLayout.Children)
            {
                if (child is View view && view != content)
                {
                    view.IsVisible = false;
                }
            }

            // Show the selected content
            content.IsVisible = true;
        }

        // Resizable Behavior
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
