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
            Rectangle area = Screen.PrimaryScreen == null ? new Rectangle(0, 0, 1280, 800) : Screen.PrimaryScreen.WorkingArea;
            int w = Math.Min(Ui.S(width), Math.Max(Ui.S(420), area.Width - Ui.S(80)));
            int h = Math.Min(Ui.S(height), Math.Max(Ui.S(320), area.Height - Ui.S(80)));
            form.StartPosition = FormStartPosition.CenterParent;
            form.MinimumSize = new Size(Ui.S(360), Ui.S(260));
            form.ClientSize = new Size(w, h);
        }

        /// <summary>在对话框底部放一条右对齐的按钮栏，返回承载面板。</summary>
        public static Panel BottomBar(Form form, params Button[] buttons)
        {
            Panel bar = new Panel();
            bar.Dock = DockStyle.Bottom;
            bar.Height = Ui.S(56);
            bar.BackColor = Color.White;
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Right;
            flow.FlowDirection = FlowDirection.RightToLeft;
            flow.WrapContents = false;
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
