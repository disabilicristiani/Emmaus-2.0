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

namespace Listbox
{
    partial class FormMappaConcettuale
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
            panelHeader = new Panel();
            label_titoloMappa = new Label();
            panelPulsanti = new FlowLayoutPanel();
            button_zoomPiu = new Button();
            button_zoomMeno = new Button();
            button_adatta = new Button();
            pannelloMappa = new PannelloMappaConcettuale();
            panelHeader.SuspendLayout();
            panelPulsanti.SuspendLayout();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.AutoSize = true;
            panelHeader.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panelHeader.BackColor = Color.FromArgb(18, 20, 40);
            panelHeader.Controls.Add(label_titoloMappa);
            panelHeader.Controls.Add(panelPulsanti);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Margin = new Padding(2, 2, 2, 2);
            panelHeader.MinimumSize = new Size(0, 49);
            panelHeader.Name = "panelHeader";
            panelHeader.Padding = new Padding(12, 8, 12, 8);
            panelHeader.Size = new Size(800, 49);
            panelHeader.TabIndex = 0;
            // 
            // label_titoloMappa
            // 
            label_titoloMappa.AutoEllipsis = true;
            label_titoloMappa.Dock = DockStyle.Fill;
            label_titoloMappa.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            label_titoloMappa.ForeColor = Color.FromArgb(225, 225, 245);
            label_titoloMappa.Location = new Point(12, 8);
            label_titoloMappa.Margin = new Padding(2, 0, 2, 0);
            label_titoloMappa.Name = "label_titoloMappa";
            label_titoloMappa.Size = new Size(566, 33);
            label_titoloMappa.TabIndex = 0;
            label_titoloMappa.Text = "Mappa dei riferimenti incrociati";
            label_titoloMappa.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panelPulsanti
            // 
            panelPulsanti.AutoSize = true;
            panelPulsanti.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panelPulsanti.Controls.Add(button_zoomPiu);
            panelPulsanti.Controls.Add(button_zoomMeno);
            panelPulsanti.Controls.Add(button_adatta);
            panelPulsanti.Dock = DockStyle.Right;
            panelPulsanti.Location = new Point(578, 8);
            panelPulsanti.Margin = new Padding(2, 2, 2, 2);
            panelPulsanti.Name = "panelPulsanti";
            panelPulsanti.Size = new Size(210, 33);
            panelPulsanti.TabIndex = 1;
            panelPulsanti.WrapContents = false;
            // 
            // button_zoomPiu
            // 
            button_zoomPiu.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            button_zoomPiu.Location = new Point(3, 0);
            button_zoomPiu.Margin = new Padding(3, 0, 3, 0);
            button_zoomPiu.Name = "button_zoomPiu";
            button_zoomPiu.Size = new Size(64, 32);
            button_zoomPiu.TabIndex = 0;
            button_zoomPiu.Text = "🔍⤢";
            button_zoomPiu.UseVisualStyleBackColor = true;
            // 
            // button_zoomMeno
            // 
            button_zoomMeno.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            button_zoomMeno.Location = new Point(73, 0);
            button_zoomMeno.Margin = new Padding(3, 0, 3, 0);
            button_zoomMeno.Name = "button_zoomMeno";
            button_zoomMeno.Size = new Size(64, 32);
            button_zoomMeno.TabIndex = 1;
            button_zoomMeno.Text = "🔍⤡";
            button_zoomMeno.UseVisualStyleBackColor = true;
            // 
            // button_adatta
            // 
            button_adatta.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            button_adatta.Location = new Point(143, 0);
            button_adatta.Margin = new Padding(3, 0, 3, 0);
            button_adatta.Name = "button_adatta";
            button_adatta.Size = new Size(64, 32);
            button_adatta.TabIndex = 2;
            button_adatta.Text = "🔍 ↔";
            button_adatta.UseVisualStyleBackColor = true;
            // 
            // pannelloMappa
            // 
            pannelloMappa.BackColor = Color.FromArgb(10, 12, 26);
            pannelloMappa.Dock = DockStyle.Fill;
            pannelloMappa.Location = new Point(0, 49);
            pannelloMappa.Margin = new Padding(2, 2, 2, 2);
            pannelloMappa.Name = "pannelloMappa";
            pannelloMappa.Size = new Size(800, 519);
            pannelloMappa.TabIndex = 1;
            // 
            // FormMappaConcettuale
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 568);
            Controls.Add(pannelloMappa);
            Controls.Add(panelHeader);
            Margin = new Padding(2, 2, 2, 2);
            MinimumSize = new Size(405, 336);
            Name = "FormMappaConcettuale";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Emmaus 2.0 - Mappa";
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            panelPulsanti.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panelHeader;
        private Label label_titoloMappa;
        private FlowLayoutPanel panelPulsanti;
        private Button button_zoomPiu;
        private Button button_zoomMeno;
        private Button button_adatta;
        private PannelloMappaConcettuale pannelloMappa;
    }
}
