using System;
using System.Drawing;
using System.Windows.Forms;

namespace SRLogbook
{
    /// <summary>6 位数字 PIN 输入框；confirm=true 时要求输入两次。</summary>
    public static class PinDialog
    {
        public static string Ask(IWin32Window owner, string title, string hint, bool confirm)
        {
            string result = null;
            using (Form form = new Form())
            {
                form.Text = Lang.T(title);
                form.BackColor = Color.White;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ShowInTaskbar = false;
                Dlg.Fit(form, 480, confirm ? 330 : 270);

                Label tip = new Label();
                tip.Text = Lang.T(hint);
                tip.Font = Theme.F(9f);
                tip.ForeColor = Theme.TextMuted;
                tip.SetBounds(Ui.S(20), Ui.S(14), Ui.S(430), Ui.S(44));
                form.Controls.Add(tip);

                Label l1 = new Label();
                l1.Text = "6 位 PIN / 6-digit PIN";
                l1.Font = Theme.F(9f);
                l1.ForeColor = Theme.TextMain;
                l1.SetBounds(Ui.S(20), Ui.S(64), Ui.S(150), Ui.S(24));
                form.Controls.Add(l1);

                TextBox first = PinBox();
                first.SetBounds(Ui.S(176), Ui.S(60), Ui.S(150), Ui.S(28));
                form.Controls.Add(first);

                TextBox second = null;
                if (confirm)
                {
                    Label l2 = new Label();
                    l2.Text = "再次输入 / Confirm";
                    l2.Font = Theme.F(9f);
                    l2.ForeColor = Theme.TextMain;
                    l2.SetBounds(Ui.S(20), Ui.S(104), Ui.S(150), Ui.S(24));
                    form.Controls.Add(l2);
                    second = PinBox();
                    second.SetBounds(Ui.S(176), Ui.S(100), Ui.S(150), Ui.S(28));
                    form.Controls.Add(second);
                }

                Label error = new Label();
                error.Text = "";
                error.Font = Theme.F(8.5f);
                error.ForeColor = Color.FromArgb(190, 60, 50);
                error.SetBounds(Ui.S(20), Ui.S(confirm ? 142 : 100), Ui.S(430), Ui.S(40));
                form.Controls.Add(error);

                Button ok = Dlg.Button("确定 / OK");
                Button cancel = Dlg.Button("取消 / Cancel");
                ok.Click += delegate
                {
                    string pin = (first.Text ?? "").Trim();
                    if (!RememberedLogin.ValidPin(pin))
                    {
                        error.Text = "请输入 6 位数字 / Enter exactly 6 digits.";
                        first.Focus();
                        return;
                    }
                    if (second != null && second.Text.Trim() != pin)
                    {
                        error.Text = "两次输入不一致 / The two PINs do not match.";
                        second.Focus();
                        return;
                    }
                    result = pin;
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                };
                cancel.Click += delegate { form.Close(); };
                Dlg.BottomBar(form, ok, cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;
                form.Shown += delegate { first.Focus(); };
                if (form.ShowDialog(owner) != DialogResult.OK) result = null;
            }
            return result;
        }

        private static TextBox PinBox()
        {
            TextBox box = new TextBox();
            box.MaxLength = 6;
            box.PasswordChar = '●';
            box.TextAlign = HorizontalAlignment.Center;
            box.Font = Theme.F(12f);
            box.ShortcutsEnabled = true;
            box.KeyPress += delegate(object s, KeyPressEventArgs e)
            {
                if (char.IsControl(e.KeyChar)) return;
                if (e.KeyChar < '0' || e.KeyChar > '9') e.Handled = true;
            };
            return box;
        }
    }
}
