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
    public partial class FormMANUTENZIONE
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
            panelTemi = new Panel();
            flowTemi = new FlowLayoutPanel();
            radioTema1 = new RadioButton();
            radioTema2 = new RadioButton();
            radioTema3 = new RadioButton();
            radioTema4 = new RadioButton();
            radioTema5 = new RadioButton();
            labelTema = new Label();
            panel1 = new Panel();
            button1 = new Button();
            checkBox4 = new CheckBox();
            checkBox3 = new CheckBox();
            label1 = new Label();
            panel2 = new Panel();
            button2 = new Button();
            label2 = new Label();
            label3 = new Label();
            checkBox9 = new CheckBox();
            checkBox7 = new CheckBox();
            panel3 = new Panel();
            checkBox11 = new CheckBox();
            button3 = new Button();
            buttonRestore = new Button();
            label4 = new Label();
            panel4 = new Panel();
            chkRipCommenti = new CheckBox();
            chkRipCronologia = new CheckBox();
            buttonEliminaPuntoRipristino = new Button();
            comboPuntiRipristino = new ComboBox();
            panelTemi.SuspendLayout();
            flowTemi.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            panel4.SuspendLayout();
            SuspendLayout();
            // 
            // panelTemi
            // 
            panelTemi.Controls.Add(flowTemi);
            panelTemi.Controls.Add(labelTema);
            panelTemi.Location = new Point(10, 10);
            panelTemi.Margin = new Padding(2);
            panelTemi.Name = "panelTemi";
            panelTemi.Size = new Size(647, 91);
            panelTemi.TabIndex = 7;
            // 
            // flowTemi
            // 
            flowTemi.AutoSize = true;
            flowTemi.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowTemi.Controls.Add(radioTema1);
            flowTemi.Controls.Add(radioTema2);
            flowTemi.Controls.Add(radioTema3);
            flowTemi.Controls.Add(radioTema4);
            flowTemi.Controls.Add(radioTema5);
            flowTemi.Location = new Point(88, 11);
            flowTemi.Margin = new Padding(2);
            flowTemi.MaximumSize = new Size(552, 0);
            flowTemi.Name = "flowTemi";
            flowTemi.Size = new Size(536, 73);
            flowTemi.TabIndex = 1;
            // 
            // radioTema1
            // 
            radioTema1.AutoSize = true;
            radioTema1.Location = new Point(0, 0);
            radioTema1.Margin = new Padding(0, 0, 22, 5);
            radioTema1.Name = "radioTema1";
            radioTema1.Size = new Size(145, 34);
            radioTema1.TabIndex = 0;
            radioTema1.TabStop = true;
            radioTema1.Text = "Notte e oro";
            radioTema1.UseVisualStyleBackColor = true;
            // 
            // radioTema2
            // 
            radioTema2.AutoSize = true;
            radioTema2.Location = new Point(167, 0);
            radioTema2.Margin = new Padding(0, 0, 22, 5);
            radioTema2.Name = "radioTema2";
            radioTema2.Size = new Size(142, 34);
            radioTema2.TabIndex = 1;
            radioTema2.Text = "Pergamena";
            radioTema2.UseVisualStyleBackColor = true;
            // 
            // radioTema3
            // 
            radioTema3.AutoSize = true;
            radioTema3.Location = new Point(331, 0);
            radioTema3.Margin = new Padding(0, 0, 0, 5);
            radioTema3.Name = "radioTema3";
            radioTema3.Size = new Size(205, 34);
            radioTema3.TabIndex = 2;
            radioTema3.Text = "Porpora e argento";
            radioTema3.UseVisualStyleBackColor = true;
            // 
            // radioTema4
            // 
            radioTema4.AutoSize = true;
            radioTema4.Location = new Point(0, 39);
            radioTema4.Margin = new Padding(0, 0, 22, 0);
            radioTema4.Name = "radioTema4";
            radioTema4.Size = new Size(220, 34);
            radioTema4.TabIndex = 3;
            radioTema4.Text = "Riposo per gli occhi";
            radioTema4.UseVisualStyleBackColor = true;
            // 
            // radioTema5
            // 
            radioTema5.AutoSize = true;
            radioTema5.Location = new Point(242, 39);
            radioTema5.Margin = new Padding(0);
            radioTema5.Name = "radioTema5";
            radioTema5.Size = new Size(167, 34);
            radioTema5.TabIndex = 4;
            radioTema5.Text = "Luce notturna";
            radioTema5.UseVisualStyleBackColor = true;
            // 
            // labelTema
            // 
            labelTema.AutoSize = true;
            labelTema.Location = new Point(10, 12);
            labelTema.Margin = new Padding(2, 0, 2, 0);
            labelTema.Name = "labelTema";
            labelTema.Size = new Size(62, 30);
            labelTema.TabIndex = 0;
            labelTema.Text = "Tema";
            // 
            // panel1
            // 
            panel1.Controls.Add(button1);
            panel1.Controls.Add(checkBox4);
            panel1.Controls.Add(checkBox3);
            panel1.Controls.Add(label1);
            panel1.Location = new Point(10, 120);
            panel1.Margin = new Padding(2);
            panel1.Name = "panel1";
            panel1.Size = new Size(280, 195);
            panel1.TabIndex = 0;
            // 
            // button1
            // 
            button1.Location = new Point(88, 139);
            button1.Margin = new Padding(2);
            button1.Name = "button1";
            button1.Size = new Size(100, 36);
            button1.TabIndex = 5;
            button1.Text = "Backup";
            button1.UseVisualStyleBackColor = true;
            // 
            // checkBox4
            // 
            checkBox4.AutoSize = true;
            checkBox4.Location = new Point(10, 90);
            checkBox4.Margin = new Padding(2);
            checkBox4.Name = "checkBox4";
            checkBox4.Size = new Size(213, 34);
            checkBox4.TabIndex = 4;
            checkBox4.Text = "Backup Cronologia";
            checkBox4.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            checkBox3.AutoSize = true;
            checkBox3.Location = new Point(10, 52);
            checkBox3.Margin = new Padding(2);
            checkBox3.Name = "checkBox3";
            checkBox3.Size = new Size(241, 34);
            checkBox3.TabIndex = 3;
            checkBox3.Text = "Backup DB Commenti";
            checkBox3.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 11);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(80, 30);
            label1.TabIndex = 0;
            label1.Text = "Backup";
            // 
            // panel2
            // 
            panel2.Controls.Add(button2);
            panel2.Controls.Add(label2);
            panel2.Location = new Point(309, 120);
            panel2.Margin = new Padding(2);
            panel2.Name = "panel2";
            panel2.Size = new Size(347, 195);
            panel2.TabIndex = 6;
            // 
            // button2
            // 
            button2.Location = new Point(95, 139);
            button2.Margin = new Padding(2);
            button2.Name = "button2";
            button2.Size = new Size(100, 36);
            button2.TabIndex = 7;
            button2.Text = "Esporta";
            button2.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(10, 11);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(226, 30);
            label2.TabIndex = 0;
            label2.Text = "Esporta miei commenti";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(10, 11);
            label3.Margin = new Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new Size(80, 30);
            label3.TabIndex = 0;
            label3.Text = "Elimina";
            // 
            // checkBox9
            // 
            checkBox9.AutoSize = true;
            checkBox9.Location = new Point(10, 60);
            checkBox9.Margin = new Padding(2);
            checkBox9.Name = "checkBox9";
            checkBox9.Size = new Size(213, 34);
            checkBox9.TabIndex = 1;
            checkBox9.Text = "Elimina Cronologia";
            checkBox9.UseVisualStyleBackColor = true;
            // 
            // checkBox7
            // 
            checkBox7.AutoSize = true;
            checkBox7.Location = new Point(10, 98);
            checkBox7.Margin = new Padding(2);
            checkBox7.Name = "checkBox7";
            checkBox7.Size = new Size(241, 34);
            checkBox7.TabIndex = 4;
            checkBox7.Text = "Elimina DB Commenti";
            checkBox7.UseVisualStyleBackColor = true;
            // 
            // panel3
            // 
            panel3.Controls.Add(checkBox11);
            panel3.Controls.Add(button3);
            panel3.Controls.Add(checkBox7);
            panel3.Controls.Add(checkBox9);
            panel3.Controls.Add(label3);
            panel3.Location = new Point(309, 328);
            panel3.Margin = new Padding(2);
            panel3.Name = "panel3";
            panel3.Size = new Size(348, 267);
            panel3.TabIndex = 6;
            // 
            // checkBox11
            // 
            checkBox11.AutoSize = true;
            checkBox11.Location = new Point(10, 136);
            checkBox11.Margin = new Padding(2);
            checkBox11.Name = "checkBox11";
            checkBox11.Size = new Size(253, 34);
            checkBox11.TabIndex = 7;
            checkBox11.Text = "Elimina Punti Ripristino";
            checkBox11.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            button3.Location = new Point(66, 212);
            button3.Margin = new Padding(2);
            button3.Name = "button3";
            button3.Size = new Size(191, 36);
            button3.TabIndex = 6;
            button3.Text = "❗ELIMINA❗";
            button3.UseVisualStyleBackColor = true;
            // 
            // buttonRestore
            // 
            buttonRestore.Location = new Point(29, 171);
            buttonRestore.Margin = new Padding(2);
            buttonRestore.Name = "buttonRestore";
            buttonRestore.Size = new Size(223, 36);
            buttonRestore.TabIndex = 5;
            buttonRestore.Text = "Ripristina Punto";
            buttonRestore.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(10, 11);
            label4.Margin = new Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new Size(100, 30);
            label4.TabIndex = 0;
            label4.Text = "Ripristino";
            // 
            // panel4
            // 
            panel4.Controls.Add(chkRipCommenti);
            panel4.Controls.Add(chkRipCronologia);
            panel4.Controls.Add(buttonEliminaPuntoRipristino);
            panel4.Controls.Add(comboPuntiRipristino);
            panel4.Controls.Add(buttonRestore);
            panel4.Controls.Add(label4);
            panel4.Location = new Point(10, 328);
            panel4.Margin = new Padding(2);
            panel4.Name = "panel4";
            panel4.Size = new Size(280, 267);
            panel4.TabIndex = 6;
            // 
            // chkRipCommenti
            // 
            chkRipCommenti.AutoSize = true;
            chkRipCommenti.Location = new Point(10, 126);
            chkRipCommenti.Margin = new Padding(2);
            chkRipCommenti.Name = "chkRipCommenti";
            chkRipCommenti.Size = new Size(168, 34);
            chkRipCommenti.TabIndex = 10;
            chkRipCommenti.Text = "DB Commenti";
            chkRipCommenti.UseVisualStyleBackColor = true;
            // 
            // chkRipCronologia
            // 
            chkRipCronologia.AutoSize = true;
            chkRipCronologia.Location = new Point(10, 90);
            chkRipCronologia.Margin = new Padding(2);
            chkRipCronologia.Name = "chkRipCronologia";
            chkRipCronologia.Size = new Size(140, 34);
            chkRipCronologia.TabIndex = 8;
            chkRipCronologia.Text = "Cronologia";
            chkRipCronologia.UseVisualStyleBackColor = true;
            // 
            // buttonEliminaPuntoRipristino
            // 
            buttonEliminaPuntoRipristino.Location = new Point(19, 212);
            buttonEliminaPuntoRipristino.Margin = new Padding(2);
            buttonEliminaPuntoRipristino.Name = "buttonEliminaPuntoRipristino";
            buttonEliminaPuntoRipristino.Size = new Size(242, 36);
            buttonEliminaPuntoRipristino.TabIndex = 7;
            buttonEliminaPuntoRipristino.Text = "❗ELIMINA PUNTO❗";
            buttonEliminaPuntoRipristino.UseVisualStyleBackColor = true;
            // 
            // comboPuntiRipristino
            // 
            comboPuntiRipristino.FormattingEnabled = true;
            comboPuntiRipristino.Location = new Point(10, 49);
            comboPuntiRipristino.Margin = new Padding(2);
            comboPuntiRipristino.Name = "comboPuntiRipristino";
            comboPuntiRipristino.Size = new Size(262, 38);
            comboPuntiRipristino.TabIndex = 6;
            // 
            // FormMANUTENZIONE
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(666, 605);
            Controls.Add(panel4);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(panelTemi);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(2);
            MaximizeBox = false;
            Name = "FormMANUTENZIONE";
            Text = "Emmaus 2.0 - Settings";
            panelTemi.ResumeLayout(false);
            panelTemi.PerformLayout();
            flowTemi.ResumeLayout(false);
            flowTemi.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelTemi;
    private Label labelTema;
    private FlowLayoutPanel flowTemi;
    private RadioButton radioTema1;
    private RadioButton radioTema2;
    private RadioButton radioTema3;
    private RadioButton radioTema4;
    private RadioButton radioTema5;
    private Panel panel1;
    private Label label1;
    private CheckBox checkBox4;
    private CheckBox checkBox3;
    private Button button1;
    private Panel panel2;
    private Button button2;
    private Label label2;
    private Label label3;
    private CheckBox checkBox9;
    private CheckBox checkBox7;
    private Panel panel3;
    private Button button3;
        private Button buttonRestore;
        private Label label4;
        private Panel panel4;
        private ComboBox comboPuntiRipristino;
        private Button buttonEliminaPuntoRipristino;
        private CheckBox checkBox11;
        private CheckBox chkRipCronologia;
        private CheckBox chkRipCommenti;
    }
}
