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

namespace Input
{
    public partial class FormINFO
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormINFO));
            panelScorrimento = new Panel();
            tabellaContenuto = new TableLayoutPanel();
            pictureBoxLogo = new PictureBox();
            labelTitolo = new Label();
            labelSottotitolo = new Label();
            flowCopyright = new FlowLayoutPanel();
            labelCopyright = new Label();
            linkEmail = new Label();
            labelTrattino = new Label();
            labelSlogan = new Label();
            labelLicenza = new Label();
            separatoreNome = new SeparatoreElegante();
            labelNome = new Label();
            linkNome = new LinkLabel();
            labelTitoloFunzionalita = new Label();
            labelFunzionalita = new Label();
            labelTitoloFeatures = new Label();
            labelFeatures = new Label();
            panelScorrimento.SuspendLayout();
            tabellaContenuto.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).BeginInit();
            flowCopyright.SuspendLayout();
            SuspendLayout();
            // 
            // panelScorrimento
            // 
            panelScorrimento.AutoScroll = true;
            panelScorrimento.Controls.Add(tabellaContenuto);
            panelScorrimento.Dock = DockStyle.Fill;
            panelScorrimento.Location = new Point(0, 0);
            panelScorrimento.Margin = new Padding(2, 2, 2, 2);
            panelScorrimento.Name = "panelScorrimento";
            panelScorrimento.Size = new Size(640, 730);
            panelScorrimento.TabIndex = 0;
            // 
            // tabellaContenuto
            // 
            tabellaContenuto.AutoSize = true;
            tabellaContenuto.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tabellaContenuto.ColumnCount = 1;
            tabellaContenuto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tabellaContenuto.Controls.Add(pictureBoxLogo, 0, 0);
            tabellaContenuto.Controls.Add(labelTitolo, 0, 1);
            tabellaContenuto.Controls.Add(labelSottotitolo, 0, 2);
            tabellaContenuto.Controls.Add(flowCopyright, 0, 3);
            tabellaContenuto.Controls.Add(labelLicenza, 0, 4);
            tabellaContenuto.Controls.Add(separatoreNome, 0, 5);
            tabellaContenuto.Controls.Add(labelNome, 0, 6);
            tabellaContenuto.Controls.Add(linkNome, 0, 7);
            tabellaContenuto.Controls.Add(labelTitoloFunzionalita, 0, 8);
            tabellaContenuto.Controls.Add(labelFunzionalita, 0, 9);
            tabellaContenuto.Controls.Add(labelTitoloFeatures, 0, 10);
            tabellaContenuto.Controls.Add(labelFeatures, 0, 11);
            tabellaContenuto.Dock = DockStyle.Top;
            tabellaContenuto.Location = new Point(0, 0);
            tabellaContenuto.Margin = new Padding(2, 2, 2, 2);
            tabellaContenuto.Name = "tabellaContenuto";
            tabellaContenuto.Padding = new Padding(16, 16, 16, 16);
            tabellaContenuto.RowCount = 12;
            tabellaContenuto.RowStyles.Add(new RowStyle());
            tabellaContenuto.RowStyles.Add(new RowStyle());
            tabellaContenuto.RowStyles.Add(new RowStyle());
            tabellaContenuto.RowStyles.Add(new RowStyle());
            tabellaContenuto.RowStyles.Add(new RowStyle());
            tabellaContenuto.RowStyles.Add(new RowStyle());
            tabellaContenuto.RowStyles.Add(new RowStyle());
            tabellaContenuto.RowStyles.Add(new RowStyle());
            tabellaContenuto.RowStyles.Add(new RowStyle());
            tabellaContenuto.RowStyles.Add(new RowStyle());
            tabellaContenuto.RowStyles.Add(new RowStyle());
            tabellaContenuto.RowStyles.Add(new RowStyle());
            tabellaContenuto.Size = new Size(610, 1373);
            tabellaContenuto.TabIndex = 0;
            // 
            // pictureBoxLogo
            // 
            pictureBoxLogo.Anchor = AnchorStyles.None;
            pictureBoxLogo.Location = new Point(225, 18);
            pictureBoxLogo.Margin = new Padding(2, 2, 2, 10);
            pictureBoxLogo.Name = "pictureBoxLogo";
            pictureBoxLogo.Size = new Size(160, 162);
            pictureBoxLogo.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxLogo.TabIndex = 0;
            pictureBoxLogo.TabStop = false;
            // 
            // labelTitolo
            // 
            labelTitolo.Anchor = AnchorStyles.None;
            labelTitolo.AutoSize = true;
            labelTitolo.Location = new Point(242, 190);
            labelTitolo.Margin = new Padding(2, 0, 2, 5);
            labelTitolo.Name = "labelTitolo";
            labelTitolo.Size = new Size(125, 30);
            labelTitolo.TabIndex = 1;
            labelTitolo.Text = "Emmaus 2.0";
            labelTitolo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelSottotitolo
            // 
            labelSottotitolo.Anchor = AnchorStyles.None;
            labelSottotitolo.AutoSize = true;
            labelSottotitolo.Location = new Point(32, 225);
            labelSottotitolo.Margin = new Padding(2, 0, 2, 15);
            labelSottotitolo.Name = "labelSottotitolo";
            labelSottotitolo.Size = new Size(546, 60);
            labelSottotitolo.TabIndex = 2;
            labelSottotitolo.Text = "Emmaus 2.0 è un programma per Windows per meditare la Sacra Scrittura.";
            labelSottotitolo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // flowCopyright
            // 
            flowCopyright.AutoSize = true;
            flowCopyright.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowCopyright.Controls.Add(labelCopyright);
            flowCopyright.Controls.Add(linkEmail);
            flowCopyright.Controls.Add(labelTrattino);
            flowCopyright.Controls.Add(labelSlogan);
            flowCopyright.Location = new Point(18, 300);
            flowCopyright.Margin = new Padding(2, 0, 2, 5);
            flowCopyright.Name = "flowCopyright";
            flowCopyright.Size = new Size(538, 62);
            flowCopyright.TabIndex = 3;
            // 
            // labelCopyright
            // 
            labelCopyright.AutoSize = true;
            labelCopyright.Location = new Point(0, 0);
            labelCopyright.Margin = new Padding(0);
            labelCopyright.Name = "labelCopyright";
            labelCopyright.Size = new Size(331, 30);
            labelCopyright.TabIndex = 0;
            labelCopyright.Text = "Copyright © 2026 DisabiliCristiani ";
            // 
            // linkEmail
            // 
            linkEmail.AutoSize = true;
            linkEmail.Location = new Point(0, 30);
            linkEmail.Margin = new Padding(0);
            linkEmail.Name = "linkEmail";
            linkEmail.Size = new Size(264, 30);
            linkEmail.TabIndex = 1;
            linkEmail.Text = "disabilicristiani@gmail.com";
            // 
            // labelTrattino
            // 
            labelTrattino.AutoSize = true;
            labelTrattino.Location = new Point(264, 30);
            labelTrattino.Margin = new Padding(0);
            labelTrattino.Name = "labelTrattino";
            labelTrattino.Size = new Size(33, 30);
            labelTrattino.TabIndex = 2;
            labelTrattino.Text = " - ";
            // 
            // labelSlogan
            // 
            labelSlogan.AutoSize = true;
            labelSlogan.Location = new Point(297, 30);
            labelSlogan.Margin = new Padding(0);
            labelSlogan.Name = "labelSlogan";
            labelSlogan.Padding = new Padding(5, 1, 5, 1);
            labelSlogan.Size = new Size(241, 32);
            labelSlogan.TabIndex = 3;
            labelSlogan.Text = "Fate del bene e pregate";
            // 
            // labelLicenza
            // 
            labelLicenza.AutoSize = true;
            labelLicenza.Location = new Point(18, 367);
            labelLicenza.Margin = new Padding(2, 0, 2, 15);
            labelLicenza.Name = "labelLicenza";
            labelLicenza.Size = new Size(550, 210);
            labelLicenza.TabIndex = 4;
            labelLicenza.Text = resources.GetString("labelLicenza.Text");
            //
            // separatoreNome
            //
            separatoreNome.Dock = DockStyle.Fill;
            separatoreNome.Location = new Point(18, 577);
            separatoreNome.Margin = new Padding(2, 0, 2, 15);
            separatoreNome.Name = "separatoreNome";
            separatoreNome.Size = new Size(550, 30);
            separatoreNome.TabIndex = 5;
            separatoreNome.TabStop = false;
            // 
            // labelNome
            // 
            labelNome.Anchor = AnchorStyles.None;
            labelNome.AutoSize = true;
            labelNome.Location = new Point(18, 622);
            labelNome.Margin = new Padding(2, 0, 2, 2);
            labelNome.Name = "labelNome";
            labelNome.Size = new Size(526, 60);
            labelNome.TabIndex = 6;
            labelNome.Text = "Il nome Emmaus si ispira al cammino dei discepoli con Gesù, mentre spiega loro le Scritture";
            labelNome.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // linkNome
            // 
            linkNome.Anchor = AnchorStyles.None;
            linkNome.AutoSize = true;
            linkNome.Location = new Point(200, 687);
            linkNome.Margin = new Padding(2, 0, 2, 15);
            linkNome.Name = "linkNome";
            linkNome.Size = new Size(210, 30);
            linkNome.TabIndex = 7;
            linkNome.TabStop = true;
            linkNome.Text = "[Cf. Luca 24, 13-32]";
            linkNome.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelTitoloFunzionalita
            // 
            labelTitoloFunzionalita.AutoSize = true;
            labelTitoloFunzionalita.Location = new Point(18, 732);
            labelTitoloFunzionalita.Margin = new Padding(2, 0, 2, 5);
            labelTitoloFunzionalita.Name = "labelTitoloFunzionalita";
            labelTitoloFunzionalita.Size = new Size(125, 30);
            labelTitoloFunzionalita.TabIndex = 8;
            labelTitoloFunzionalita.Text = "Funzionalità";
            // 
            // labelFunzionalita
            // 
            labelFunzionalita.AutoSize = true;
            labelFunzionalita.Location = new Point(18, 767);
            labelFunzionalita.Margin = new Padding(2, 0, 2, 15);
            labelFunzionalita.Name = "labelFunzionalita";
            labelFunzionalita.Size = new Size(571, 300);
            labelFunzionalita.TabIndex = 9;
            labelFunzionalita.Text = resources.GetString("labelFunzionalita.Text");
            // 
            // labelTitoloFeatures
            // 
            labelTitoloFeatures.AutoSize = true;
            labelTitoloFeatures.Location = new Point(18, 1082);
            labelTitoloFeatures.Margin = new Padding(2, 0, 2, 5);
            labelTitoloFeatures.Name = "labelTitoloFeatures";
            labelTitoloFeatures.Size = new Size(91, 30);
            labelTitoloFeatures.TabIndex = 10;
            labelTitoloFeatures.Text = "Features";
            // 
            // labelFeatures
            // 
            labelFeatures.AutoSize = true;
            labelFeatures.Location = new Point(18, 1117);
            labelFeatures.Margin = new Padding(2, 0, 2, 0);
            labelFeatures.Name = "labelFeatures";
            labelFeatures.Size = new Size(571, 240);
            labelFeatures.TabIndex = 11;
            labelFeatures.Text = resources.GetString("labelFeatures.Text");
            // 
            // FormINFO
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(640, 730);
            Controls.Add(panelScorrimento);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(2, 2, 2, 2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormINFO";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Emmaus 2.0 - About";
            panelScorrimento.ResumeLayout(false);
            panelScorrimento.PerformLayout();
            tabellaContenuto.ResumeLayout(false);
            tabellaContenuto.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).EndInit();
            flowCopyright.ResumeLayout(false);
            flowCopyright.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelScorrimento;
        private TableLayoutPanel tabellaContenuto;
        private PictureBox pictureBoxLogo;
        private Label labelTitolo;
        private Label labelSottotitolo;
        private FlowLayoutPanel flowCopyright;
        private Label labelCopyright;
        private Label linkEmail;
        private Label labelTrattino;
        private Label labelSlogan;
        private Label labelLicenza;
        private SeparatoreElegante separatoreNome;
        private Label labelNome;
        private LinkLabel linkNome;
        private Label labelTitoloFunzionalita;
        private Label labelFunzionalita;
        private Label labelTitoloFeatures;
        private Label labelFeatures;
    }
}
