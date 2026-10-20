using System.Windows.Forms;

namespace SRLogbook
{
    /// <summary>MessageBox 的中英文包装：显示前按当前语言翻译文本与标题。</summary>
    public static class Msg
    {
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return MessageBox.Show(Lang.T(text), Lang.T(caption), buttons, icon);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
            return MessageBox.Show(Lang.T(text), Lang.T(caption), buttons);
        }

        public static DialogResult Show(string text, string caption)
        {
            return MessageBox.Show(Lang.T(text), Lang.T(caption));
        }

        public static DialogResult Show(string text)
        {
            return MessageBox.Show(Lang.T(text));
        }
    }
}
