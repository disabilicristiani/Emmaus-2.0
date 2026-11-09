// MIT License
//
// Copyright (c) 2026 DisabiliCristiani <disabilicristiani@gmail.com> - Fate del bene e pregate
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace emmaus_v2
{
    partial class FormTIMER
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            tableLayoutPanel1 = new TableLayoutPanel();
            pictureBoxGesu = new PictureBox();
            labelTimer = new Label();
            flowLayoutPanelPulsanti = new FlowLayoutPanel();
            button_prayGesu = new Button();
            buttonPlayPausa = new Button();
            buttonStop = new Button();
            button_prayMaria = new Button();
            pictureBoxMaria = new PictureBox();
            timerAggiorna = new System.Windows.Forms.Timer(components);
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxGesu).BeginInit();
            flowLayoutPanelPulsanti.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxMaria).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.Controls.Add(pictureBoxGesu, 0, 0);
            tableLayoutPanel1.Controls.Add(labelTimer, 1, 0);
            tableLayoutPanel1.Controls.Add(flowLayoutPanelPulsanti, 1, 1);
            tableLayoutPanel1.Controls.Add(pictureBoxMaria, 2, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(2, 2, 2, 2);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(2, 2, 2, 2);
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            tableLayoutPanel1.Size = new Size(436, 113);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // pictureBoxGesu
            // 
            pictureBoxGesu.Dock = DockStyle.Fill;
            pictureBoxGesu.Location = new Point(4, 4);
            pictureBoxGesu.Margin = new Padding(2, 2, 2, 2);
            pictureBoxGesu.Name = "pictureBoxGesu";
            tableLayoutPanel1.SetRowSpan(pictureBoxGesu, 2);
            pictureBoxGesu.Size = new Size(82, 105);
            pictureBoxGesu.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxGesu.TabIndex = 0;
            pictureBoxGesu.TabStop = false;
            // 
            // labelTimer
            // 
            labelTimer.Dock = DockStyle.Fill;
            labelTimer.Font = new Font("Consolas", 18F, FontStyle.Bold);
            labelTimer.Location = new Point(90, 2);
            labelTimer.Margin = new Padding(2, 0, 2, 0);
            labelTimer.Name = "labelTimer";
            labelTimer.Size = new Size(255, 65);
            labelTimer.TabIndex = 2;
            labelTimer.Text = "00:00:00";
            labelTimer.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanelPulsanti
            // 
            flowLayoutPanelPulsanti.Anchor = AnchorStyles.None;
            flowLayoutPanelPulsanti.AutoSize = true;
            flowLayoutPanelPulsanti.Controls.Add(button_prayGesu);
            flowLayoutPanelPulsanti.Controls.Add(buttonPlayPausa);
            flowLayoutPanelPulsanti.Controls.Add(buttonStop);
            flowLayoutPanelPulsanti.Controls.Add(button_prayMaria);
            flowLayoutPanelPulsanti.Location = new Point(93, 70);
            flowLayoutPanelPulsanti.Margin = new Padding(2, 2, 2, 2);
            flowLayoutPanelPulsanti.Name = "flowLayoutPanelPulsanti";
            flowLayoutPanelPulsanti.Size = new Size(248, 38);
            flowLayoutPanelPulsanti.TabIndex = 3;
            flowLayoutPanelPulsanti.WrapContents = false;
            // 
            // button_prayGesu
            // 
            button_prayGesu.Font = new Font("Segoe UI Symbol", 17F);
            button_prayGesu.Location = new Point(2, 2);
            button_prayGesu.Margin = new Padding(2, 2, 2, 2);
            button_prayGesu.Name = "button_prayGesu";
            button_prayGesu.Size = new Size(39, 34);
            button_prayGesu.TabIndex = 3;
            button_prayGesu.Text = "✝";
            button_prayGesu.UseVisualStyleBackColor = true;
            // 
            // buttonPlayPausa
            // 
            buttonPlayPausa.Font = new Font("Segoe UI Symbol", 12F);
            buttonPlayPausa.Location = new Point(45, 2);
            buttonPlayPausa.Margin = new Padding(2, 2, 2, 2);
            buttonPlayPausa.Name = "buttonPlayPausa";
            buttonPlayPausa.Size = new Size(77, 34);
            buttonPlayPausa.TabIndex = 0;
            buttonPlayPausa.Text = "▶";
            buttonPlayPausa.UseVisualStyleBackColor = true;
            buttonPlayPausa.Click += buttonPlayPausa_Click;
            // 
            // buttonStop
            // 
            buttonStop.Font = new Font("Segoe UI Symbol", 12F);
            buttonStop.Location = new Point(126, 2);
            buttonStop.Margin = new Padding(2, 2, 2, 2);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(77, 34);
            buttonStop.TabIndex = 1;
            buttonStop.Text = "⏹";
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += buttonStop_Click;
            // 
            // button_prayMaria
            // 
            button_prayMaria.Font = new Font("Segoe UI Symbol", 17F);
            button_prayMaria.Location = new Point(207, 2);
            button_prayMaria.Margin = new Padding(2, 2, 2, 2);
            button_prayMaria.Name = "button_prayMaria";
            button_prayMaria.Size = new Size(39, 34);
            button_prayMaria.TabIndex = 2;
            button_prayMaria.Text = "⚜";
            button_prayMaria.UseVisualStyleBackColor = true;
            // 
            // pictureBoxMaria
            // 
            pictureBoxMaria.Dock = DockStyle.Fill;
            pictureBoxMaria.Location = new Point(349, 4);
            pictureBoxMaria.Margin = new Padding(2, 2, 2, 2);
            pictureBoxMaria.Name = "pictureBoxMaria";
            tableLayoutPanel1.SetRowSpan(pictureBoxMaria, 2);
            pictureBoxMaria.Size = new Size(83, 105);
            pictureBoxMaria.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxMaria.TabIndex = 1;
            pictureBoxMaria.TabStop = false;
            // 
            // timerAggiorna
            // 
            timerAggiorna.Interval = 200;
            timerAggiorna.Tick += timerAggiorna_Tick;
            // 
            // FormTIMER
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(436, 113);
            Controls.Add(tableLayoutPanel1);
            Margin = new Padding(2, 2, 2, 2);
            MaximizeBox = false;
            Name = "FormTIMER";
            Text = "Emmaus 2.0";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxGesu).EndInit();
            flowLayoutPanelPulsanti.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxMaria).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private PictureBox pictureBoxGesu;
        private PictureBox pictureBoxMaria;
        private Label labelTimer;
        private FlowLayoutPanel flowLayoutPanelPulsanti;
        private Button buttonPlayPausa;
        private Button buttonStop;
        private System.Windows.Forms.Timer timerAggiorna;
        private Button button_prayGesu;
        private Button button_prayMaria;
    }
}
