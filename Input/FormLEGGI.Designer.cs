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
    partial class FormLEGGI
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
            pictureBoxHeader = new PictureBox();
            panelTesti = new Panel();
            label_riferimento = new Label();
            label_libro = new Label();
            label_categoria = new Label();
            label_testamento = new Label();
            checkBox_AT = new CheckBox();
            checkBox_NT = new CheckBox();
            bottoneEspandiAT = new Button();
            bottoneEspandiNT = new Button();
            dataGridView1 = new DataGridView();
            button_altriRisultatiRicerca = new Button();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxHeader).BeginInit();
            panelTesti.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.Controls.Add(pictureBoxHeader);
            panelHeader.Controls.Add(panelTesti);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Margin = new Padding(2, 2, 2, 2);
            panelHeader.Name = "panelHeader";
            panelHeader.Padding = new Padding(16, 12, 16, 12);
            panelHeader.Size = new Size(768, 211);
            panelHeader.TabIndex = 0;
            // 
            // pictureBoxHeader
            // 
            pictureBoxHeader.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pictureBoxHeader.Location = new Point(604, 12);
            pictureBoxHeader.Margin = new Padding(2, 2, 2, 2);
            pictureBoxHeader.Name = "pictureBoxHeader";
            pictureBoxHeader.Size = new Size(148, 186);
            pictureBoxHeader.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxHeader.TabIndex = 1;
            pictureBoxHeader.TabStop = false;
            // 
            // panelTesti
            // 
            panelTesti.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelTesti.Controls.Add(label_riferimento);
            panelTesti.Controls.Add(label_libro);
            panelTesti.Controls.Add(label_categoria);
            panelTesti.Controls.Add(label_testamento);
            panelTesti.Controls.Add(checkBox_AT);
            panelTesti.Controls.Add(checkBox_NT);
            panelTesti.Controls.Add(bottoneEspandiAT);
            panelTesti.Controls.Add(bottoneEspandiNT);
            panelTesti.Location = new Point(16, 12);
            panelTesti.Margin = new Padding(2, 2, 2, 2);
            panelTesti.Name = "panelTesti";
            panelTesti.Size = new Size(580, 186);
            panelTesti.TabIndex = 0;
            // 
            // label_riferimento
            // 
            label_riferimento.AutoSize = true;
            label_riferimento.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            label_riferimento.Location = new Point(0, 97);
            label_riferimento.Margin = new Padding(2, 0, 2, 0);
            label_riferimento.Name = "label_riferimento";
            label_riferimento.Size = new Size(314, 45);
            label_riferimento.TabIndex = 3;
            label_riferimento.Text = "Capitolo e versetto";
            // 
            // label_libro
            // 
            label_libro.AutoSize = true;
            label_libro.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            label_libro.Location = new Point(0, 65);
            label_libro.Margin = new Padding(2, 0, 2, 0);
            label_libro.Name = "label_libro";
            label_libro.Size = new Size(99, 45);
            label_libro.TabIndex = 2;
            label_libro.Text = "Libro";
            // 
            // label_categoria
            // 
            label_categoria.AutoSize = true;
            label_categoria.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            label_categoria.Location = new Point(0, 32);
            label_categoria.Margin = new Padding(2, 0, 2, 0);
            label_categoria.Name = "label_categoria";
            label_categoria.Size = new Size(170, 45);
            label_categoria.TabIndex = 1;
            label_categoria.Text = "Categoria";
            // 
            // label_testamento
            // 
            label_testamento.AutoSize = true;
            label_testamento.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            label_testamento.Location = new Point(0, 0);
            label_testamento.Margin = new Padding(2, 0, 2, 0);
            label_testamento.Name = "label_testamento";
            label_testamento.Size = new Size(201, 45);
            label_testamento.TabIndex = 0;
            label_testamento.Text = "Testamento";
            // 
            // checkBox_AT
            // 
            checkBox_AT.AutoSize = true;
            checkBox_AT.Checked = true;
            checkBox_AT.CheckState = CheckState.Checked;
            checkBox_AT.Location = new Point(2, 130);
            checkBox_AT.Margin = new Padding(2, 2, 2, 2);
            checkBox_AT.Name = "checkBox_AT";
            checkBox_AT.Size = new Size(62, 34);
            checkBox_AT.TabIndex = 4;
            checkBox_AT.Text = "AT";
            checkBox_AT.UseVisualStyleBackColor = true;
            checkBox_AT.Visible = false;
            checkBox_AT.CheckedChanged += checkBox_Testamento_CheckedChanged;
            // 
            // checkBox_NT
            // 
            checkBox_NT.AutoSize = true;
            checkBox_NT.Checked = true;
            checkBox_NT.CheckState = CheckState.Checked;
            checkBox_NT.Location = new Point(169, 130);
            checkBox_NT.Margin = new Padding(2, 2, 2, 2);
            checkBox_NT.Name = "checkBox_NT";
            checkBox_NT.Size = new Size(66, 34);
            checkBox_NT.TabIndex = 5;
            checkBox_NT.Text = "NT";
            checkBox_NT.UseVisualStyleBackColor = true;
            checkBox_NT.Visible = false;
            checkBox_NT.CheckedChanged += checkBox_Testamento_CheckedChanged;
            // 
            // bottoneEspandiAT
            // 
            bottoneEspandiAT.FlatStyle = FlatStyle.Flat;
            bottoneEspandiAT.Location = new Point(59, 130);
            bottoneEspandiAT.Margin = new Padding(2, 2, 2, 2);
            bottoneEspandiAT.Name = "bottoneEspandiAT";
            bottoneEspandiAT.Size = new Size(76, 33);
            bottoneEspandiAT.TabIndex = 6;
            bottoneEspandiAT.Text = "▾ Libri";
            bottoneEspandiAT.UseVisualStyleBackColor = true;
            bottoneEspandiAT.Visible = false;
            bottoneEspandiAT.Click += bottoneEspandiAT_Click;
            // 
            // bottoneEspandiNT
            // 
            bottoneEspandiNT.FlatStyle = FlatStyle.Flat;
            bottoneEspandiNT.Location = new Point(230, 130);
            bottoneEspandiNT.Margin = new Padding(2, 2, 2, 2);
            bottoneEspandiNT.Name = "bottoneEspandiNT";
            bottoneEspandiNT.Size = new Size(76, 33);
            bottoneEspandiNT.TabIndex = 7;
            bottoneEspandiNT.Text = "▾ Libri";
            bottoneEspandiNT.UseVisualStyleBackColor = true;
            bottoneEspandiNT.Visible = false;
            bottoneEspandiNT.Click += bottoneEspandiNT_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(0, 211);
            dataGridView1.Margin = new Padding(2, 2, 2, 2);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.RowHeadersWidth = 72;
            dataGridView1.Size = new Size(768, 564);
            dataGridView1.TabIndex = 1;
            dataGridView1.CellMouseDown += dataGridView1_CellMouseDown;
            dataGridView1.CellPainting += dataGridView1_CellPainting;
            // 
            // button_altriRisultatiRicerca
            // 
            button_altriRisultatiRicerca.Dock = DockStyle.Bottom;
            button_altriRisultatiRicerca.Location = new Point(0, 775);
            button_altriRisultatiRicerca.Margin = new Padding(2, 2, 2, 2);
            button_altriRisultatiRicerca.Name = "button_altriRisultatiRicerca";
            button_altriRisultatiRicerca.Size = new Size(768, 36);
            button_altriRisultatiRicerca.TabIndex = 2;
            button_altriRisultatiRicerca.Text = "Mostra altri 3 risultati";
            button_altriRisultatiRicerca.UseVisualStyleBackColor = true;
            button_altriRisultatiRicerca.Visible = false;
            button_altriRisultatiRicerca.Click += button_altriRisultatiRicerca_Click;
            // 
            // FormLEGGI
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(768, 811);
            Controls.Add(dataGridView1);
            Controls.Add(button_altriRisultatiRicerca);
            Controls.Add(panelHeader);
            Margin = new Padding(2, 2, 2, 2);
            Name = "FormLEGGI";
            Text = "Emmaus 2.0";
            Load += FormLEGGI_Load;
            panelHeader.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxHeader).EndInit();
            panelTesti.ResumeLayout(false);
            panelTesti.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelHeader;
        private PictureBox pictureBoxHeader;
        private Panel panelTesti;
        private Label label_testamento;
        private Label label_categoria;
        private Label label_libro;
        private Label label_riferimento;
        private CheckBox checkBox_AT;
        private CheckBox checkBox_NT;
        private Button bottoneEspandiAT;
        private Button bottoneEspandiNT;
        private DataGridView dataGridView1;
        private Button button_altriRisultatiRicerca;
    }
}
