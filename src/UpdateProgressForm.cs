using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SRLogbook
{    /// <summary>下载更新的进度窗口：显示进度条，完成后自动关闭。</summary>
    public class UpdateProgressForm : Form
    {
        private readonly Updater.Release release;
        private readonly Label label;
        private readonly ProgressBar bar;
        public string Source { get; private set; }
        public Exception Error { get; private set; }

        public UpdateProgressForm(Updater.Release rel)
        {
            release = rel;
            Text = Lang.T("正在下载更新");
            Dlg.Fit(this, 480, 220);
            BackColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            label = new Label();
            label.Text = Lang.T("正在下载 " + rel.tag_name + " …");
            label.SetBounds(Ui.S(20), Ui.S(18), Ui.S(430), Ui.S(40));
            label.Font = Theme.F(9.5f);
            label.ForeColor = Theme.TextMain;
            Controls.Add(label);
            bar = new ProgressBar();
            bar.SetBounds(Ui.S(20), Ui.S(70), Ui.S(430), Ui.S(22));
            bar.Minimum = 0; bar.Maximum = 100;
            Controls.Add(bar);
            Shown += async delegate { await Run(); };
        }

        private async Task Run()
        {
            Progress<int> progress = new Progress<int>(p => { bar.Value = Math.Max(0, Math.Min(100, p)); });
            try
            {
                Source = await Task.Run(() => Updater.DownloadAndStage(release, progress));
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                Error = ex;
                DialogResult = DialogResult.Cancel;
            }
            Close();
        }
    }
}
