namespace FMAC
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private Button btnToggle = new Button();
        private Button btnPreview = new Button();
        private PictureBox previewBox = new PictureBox();

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(576, 1024);
            this.Text = "FMAC";
            this.MaximizeBox = false;
            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            this.Controls.Add(previewBox);
            this.Controls.Add(btnToggle);
            this.Controls.Add(btnPreview);

            this.previewBox.SizeMode = PictureBoxSizeMode.StretchImage;
            this.previewBox.BackColor = Color.Transparent;

            this.btnToggle.AutoSize = true;
            this.btnToggle.Text = "Toggle";
            this.btnToggle.Click += BtnToggle_Click;

            this.btnPreview.AutoSize = true;
            this.btnPreview.Text = "Preview";
            this.btnPreview.Click += BtnPreview_Click;
        }

        #endregion
    }
}
