using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CXFLGUI
{
    public class ButtonHoverBehavior : Behavior<Button>
    {
        protected override void OnAttachedTo(Button button)
        {
            button.Pressed += OnButtonPressed;
            button.Released += OnButtonReleased;
        }

        protected override void OnDetachingFrom(Button button)
        {
            button.Pressed -= OnButtonPressed;
            button.Released -= OnButtonReleased;
        }

        private void OnButtonPressed(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackgroundColor = Color.FromHex("#2980b9");
            }
        }

        private void OnButtonReleased(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackgroundColor = Color.FromHex("#3498db");
            }
        }
    }
}
