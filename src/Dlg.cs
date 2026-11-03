using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SRLogbook
{
    /// <summary>
    /// 对话框统一布局：底部按钮条用 RightToLeft 的 FlowLayoutPanel，
    /// 不再按“尚未定宽”的父面板宽度手算坐标（那是按钮跑到屏幕外的根因）；
    /// 窗口尺寸也会被限制在屏幕工作区内，避免大字体/高 DPI 下超出显示范围。
    /// </summary>
    public static class Dlg
    {
        public static void Fit(Form form, int width, int height)
        {
            Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
            int w = Math.Min(Ui.S(width), Math.Max(Ui.S(420), area.Width - Ui.S(48)));
            int h = Math.Min(Ui.S(height), Math.Max(Ui.S(320), area.Height - Ui.S(64)));
            form.StartPosition = FormStartPosition.CenterParent;
            form.MinimumSize = new Size(Math.Min(Ui.S(360), w), Math.Min(Ui.S(220), h));
            form.ClientSize = new Size(w, h);
            // 高 DPI / 多显示器下 CenterParent 可能把对话框推到屏幕外，显示前后各收一次。
            form.Load += delegate { Clamp(form); };
            form.Shown += delegate { Clamp(form); };
        }

        /// <summary>把窗口收进它所在显示器的可用区域，避免标题栏或内容被裁掉。</summary>
        public static void Clamp(Form form)
        {
            Rectangle screen = Screen.FromControl(form).WorkingArea;
            int availableWidth = Math.Max(1, screen.Width - Ui.S(8));
            int availableHeight = Math.Max(1, screen.Height - Ui.S(8));
            form.MinimumSize = new Size(Math.Min(form.MinimumSize.Width, availableWidth), Math.Min(form.MinimumSize.Height, availableHeight));
            if (form.Width > availableWidth) form.Width = availableWidth;
            if (form.Height > availableHeight) form.Height = availableHeight;
            if (form.Top < screen.Top) form.Top = screen.Top;
            if (form.Bottom > screen.Bottom) form.Top = Math.Max(screen.Top, screen.Bottom - form.Height);
            if (form.Left < screen.Left) form.Left = screen.Left;
            if (form.Right > screen.Right) form.Left = Math.Max(screen.Left, screen.Right - form.Width);
        }

        /// <summary>在对话框底部放一条右对齐的按钮栏，返回承载面板。</summary>
        public static Panel BottomBar(Form form, params Button[] buttons)
        {
            Panel bar = new Panel();
            bar.Dock = DockStyle.Bottom;
            bar.Height = Ui.S(56);
            bar.BackColor = Color.White;
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.RightToLeft;
            flow.WrapContents = true;
            flow.AutoSize = true;
            flow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flow.BackColor = Color.White;
            flow.Padding = new Padding(0, Ui.S(12), Ui.S(12), 0);
            foreach (Button b in buttons)
            {
                b.AutoSize = true;
                b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                b.MinimumSize = new Size(Ui.S(88), Ui.S(32));
                b.Margin = new Padding(Ui.S(6), 0, 0, 0);
                b.Padding = new Padding(Ui.S(8), Ui.S(2), Ui.S(8), Ui.S(2));
                flow.Controls.Add(b);
            }
            bar.Controls.Add(flow);
            Action fitButtons = delegate
            {
                int desired = Math.Max(Ui.S(56), flow.GetPreferredSize(new Size(Math.Max(1, bar.ClientSize.Width), 0)).Height + Ui.S(8));
                if (bar.Height != desired) bar.Height = desired;
            };
            bar.SizeChanged += delegate { fitButtons(); };
            form.Shown += delegate { fitButtons(); };
            form.Controls.Add(bar);
            return bar;
        }

        public static Button Button(string text)
        {
            Button b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Theme.CardBorder;
            b.FlatAppearance.BorderSize = 1;
            b.BackColor = Color.White;
            b.ForeColor = Theme.TextMain;
            b.Font = Theme.F(9f);
            b.Cursor = Cursors.Hand;
            return b;
        }
    }
}
