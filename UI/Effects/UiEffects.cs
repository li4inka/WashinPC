using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace WashinPC.UI.Effects
{
    public static class UiEffects
    {
        public static void FadeIn(UIElement element, int ms)
        {
            if (element == null) return;

            element.Opacity = 0;

            DoubleAnimation anim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(ms),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            element.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        public static void FadeOut(UIElement element, int ms, Action completed)
        {
            if (element == null) return;

            DoubleAnimation anim = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(ms),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            if (completed != null)
                anim.Completed += delegate { completed(); };

            element.BeginAnimation(UIElement.OpacityProperty, anim);
        }
    }
}
