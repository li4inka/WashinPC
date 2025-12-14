using System.Drawing;
using System.Windows.Forms;

namespace WashinPC
{
    internal static class Theme
    {
        // Apple-like palette
        public static readonly Color Background = Color.FromArgb(245, 245, 247);
        public static readonly Color Card = Color.White;
        public static readonly Color PrimaryText = Color.FromArgb(29, 29, 31);
        public static readonly Color SecondaryText = Color.FromArgb(110, 110, 115);
        public static readonly Color Accent = Color.FromArgb(0, 122, 255);
        public static readonly Color Border = Color.FromArgb(229, 229, 234);

        public static readonly Font FontRegular = new Font("Segoe UI", 9F);
        public static readonly Font FontHeader = new Font("Segoe UI", 12F, FontStyle.Bold);
        public static readonly Font FontCardTitle = new Font("Segoe UI", 10F, FontStyle.Bold);

        public static void Apply(Form form)
        {
            form.BackColor = Background;
            form.Font = FontRegular;
            ApplyRecursive(form.Controls);
        }

        public static void Apply(UserControl control)
        {
            control.BackColor = Background;
            control.Font = FontRegular;
            ApplyRecursive(control.Controls);
        }

        public static void StylePrimaryButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Accent;
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            b.Height = 30;
        }

        public static void StyleSecondaryButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Border;
            b.ForeColor = PrimaryText;
            b.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            b.Height = 30;
        }

        public static void EnableHover(Button b)
        {
            var normal = b.BackColor;
            var hover = ControlPaint.Dark(normal, 0.08f);

            b.MouseEnter += (s, e) => b.BackColor = hover;
            b.MouseLeave += (s, e) => b.BackColor = normal;
        }

        private static void ApplyRecursive(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                if (c is Label lbl)
                {
                    lbl.ForeColor = PrimaryText;
                    lbl.BackColor = Color.Transparent;
                }
                else if (c is Button btn)
                {
                    // В Designer кнопки часто имеют дефолтный вид,
                    // поэтому трогаем только если это "наши" кнопки.
                    // Если понадобится — явная стилизация в коде страниц.
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                }
                else if (c is Panel p)
                {
                    // Панели оставляем как есть — Card/Background задаются вручную.
                }
                else
                {
                    // фон для прочих контролов (по умолчанию)
                    if (c.BackColor == SystemColors.Control)
                        c.BackColor = Background;
                }

                if (c.HasChildren)
                    ApplyRecursive(c.Controls);
            }
        }
    }
}
