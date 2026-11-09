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
    
    partial class FormINSERISCI
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
            label_libro = new Label();
            textBox_nota = new TextBox();
            button_inserisci = new Button();
            button_leggi = new Button();
            button_manutenzione = new Button();
            comboBox_capitolo = new ComboBox();
            label_capVers = new Label();
            comboBox_versettoDa = new ComboBox();
            label_virgola = new Label();
            comboBoxLibri = new ComboBox();
            comboBox_versettoFine = new ComboBox();
            label1 = new Label();
            button_cerca = new Button();
            button_random = new Button();
            button_indietro = new Button();
            button_avanti = new Button();
            SuspendLayout();
            // 
            // label_libro
            // 
            label_libro.AutoSize = true;
            label_libro.Location = new Point(10, 26);
            label_libro.Margin = new Padding(2, 0, 2, 0);
            label_libro.Name = "label_libro";
            label_libro.Size = new Size(59, 30);
            label_libro.TabIndex = 3;
            label_libro.Text = "Libro";
            label_libro.TextAlign = ContentAlignment.TopCenter;
            // 
            // textBox_nota
            // 
            textBox_nota.Location = new Point(10, 283);
            textBox_nota.Margin = new Padding(2);
            textBox_nota.Multiline = true;
            textBox_nota.Name = "textBox_nota";
            textBox_nota.PlaceholderText = "Nota da inserire o testo da cercare (la ricerca riconosce virgolette, AND/OR/|, +/-, wildcard * e intervalli N..M.)";
            textBox_nota.Size = new Size(418, 183);
            textBox_nota.TabIndex = 8;
            // 
            // button_inserisci
            // 
            button_inserisci.Location = new Point(280, 483);
            button_inserisci.Margin = new Padding(2);
            button_inserisci.Name = "button_inserisci";
            button_inserisci.Size = new Size(146, 36);
            button_inserisci.TabIndex = 10;
            button_inserisci.Text = "COMMENTA";
            button_inserisci.UseVisualStyleBackColor = true;
            button_inserisci.Click += button_inserisci_Click;
            // 
            // button_leggi
            // 
            button_leggi.Location = new Point(306, 176);
            button_leggi.Margin = new Padding(2);
            button_leggi.Name = "button_leggi";
            button_leggi.Size = new Size(120, 36);
            button_leggi.TabIndex = 5;
            button_leggi.Text = "MEDITA";
            button_leggi.UseVisualStyleBackColor = true;
            button_leggi.Click += button_leggi_Click;
            // 
            // button_manutenzione
            // 
            button_manutenzione.AccessibleDescription = "Apre manutenzione";
            button_manutenzione.AccessibleName = "Manutenzione";
            button_manutenzione.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button_manutenzione.Location = new Point(386, 538);
            button_manutenzione.Margin = new Padding(2);
            button_manutenzione.Name = "button_manutenzione";
            button_manutenzione.Size = new Size(40, 41);
            button_manutenzione.TabIndex = 11;
            button_manutenzione.UseVisualStyleBackColor = true;
            button_manutenzione.Click += button_manutenzione_Click;
            // 
            // comboBox_capitolo
            // 
            comboBox_capitolo.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox_capitolo.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_capitolo.FormattingEnabled = true;
            comboBox_capitolo.Location = new Point(10, 174);
            comboBox_capitolo.Margin = new Padding(2);
            comboBox_capitolo.Name = "comboBox_capitolo";
            comboBox_capitolo.Size = new Size(73, 36);
            comboBox_capitolo.TabIndex = 2;
            comboBox_capitolo.DrawItem += comboBox_capitolo_DrawItem;
            comboBox_capitolo.SelectedIndexChanged += comboBox_capitolo_SelectedIndexChanged;
            // 
            // label_capVers
            // 
            label_capVers.AutoSize = true;
            label_capVers.Location = new Point(10, 131);
            label_capVers.Margin = new Padding(2, 0, 2, 0);
            label_capVers.Name = "label_capVers";
            label_capVers.Size = new Size(176, 30);
            label_capVers.TabIndex = 5;
            label_capVers.Text = "Capitolo , Versetti";
            label_capVers.TextAlign = ContentAlignment.TopCenter;
            // 
            // comboBox_versettoDa
            // 
            comboBox_versettoDa.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox_versettoDa.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_versettoDa.FormattingEnabled = true;
            comboBox_versettoDa.Location = new Point(92, 174);
            comboBox_versettoDa.Margin = new Padding(2);
            comboBox_versettoDa.Name = "comboBox_versettoDa";
            comboBox_versettoDa.Size = new Size(105, 36);
            comboBox_versettoDa.TabIndex = 3;
            comboBox_versettoDa.DrawItem += comboBox_versettoDa_DrawItem;
            comboBox_versettoDa.SelectedIndexChanged += comboBox_versettoDa_SelectedIndexChanged;
            // 
            // label_virgola
            // 
            label_virgola.AutoSize = true;
            label_virgola.Location = new Point(86, 176);
            label_virgola.Margin = new Padding(2, 0, 2, 0);
            label_virgola.Name = "label_virgola";
            label_virgola.Size = new Size(18, 30);
            label_virgola.TabIndex = 8;
            label_virgola.Text = ",";
            label_virgola.TextAlign = ContentAlignment.TopCenter;
            // 
            // comboBoxLibri
            // 
            comboBoxLibri.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxLibri.FormattingEnabled = true;
            comboBoxLibri.ItemHeight = 32;
            comboBoxLibri.Location = new Point(10, 70);
            comboBoxLibri.Margin = new Padding(2);
            comboBoxLibri.MaxDropDownItems = 12;
            comboBoxLibri.Name = "comboBoxLibri";
            comboBoxLibri.Size = new Size(287, 38);
            comboBoxLibri.TabIndex = 1;
            comboBoxLibri.DrawItem += comboBoxLibri_DrawItem;
            comboBoxLibri.SelectedIndexChanged += comboBoxLibri_SelectedIndexChanged;
            // 
            // comboBox_versettoFine
            // 
            comboBox_versettoFine.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox_versettoFine.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_versettoFine.Enabled = false;
            comboBox_versettoFine.FormattingEnabled = true;
            comboBox_versettoFine.Location = new Point(224, 174);
            comboBox_versettoFine.Margin = new Padding(2);
            comboBox_versettoFine.Name = "comboBox_versettoFine";
            comboBox_versettoFine.Size = new Size(73, 36);
            comboBox_versettoFine.TabIndex = 4;
            comboBox_versettoFine.DrawItem += comboBox_versettoFine_DrawItem;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(201, 176);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(21, 30);
            label1.TabIndex = 9;
            label1.Text = "-";
            label1.TextAlign = ContentAlignment.TopCenter;
            // 
            // button_cerca
            // 
            button_cerca.Location = new Point(10, 483);
            button_cerca.Margin = new Padding(2);
            button_cerca.Name = "button_cerca";
            button_cerca.Size = new Size(120, 36);
            button_cerca.TabIndex = 9;
            button_cerca.Text = "RICERCA";
            button_cerca.UseVisualStyleBackColor = true;
            button_cerca.Click += button_cerca_Click;
            // 
            // button_random
            // 
            button_random.Location = new Point(306, 69);
            button_random.Margin = new Padding(2);
            button_random.Name = "button_random";
            button_random.Size = new Size(120, 36);
            button_random.TabIndex = 0;
            button_random.Text = "ISPIRAMI";
            button_random.UseVisualStyleBackColor = true;
            button_random.Click += button_random_Click;
            // 
            // button_indietro
            // 
            button_indietro.Enabled = false;
            button_indietro.Location = new Point(10, 225);
            button_indietro.Margin = new Padding(2);
            button_indietro.Name = "button_indietro";
            button_indietro.Size = new Size(72, 36);
            button_indietro.TabIndex = 6;
            button_indietro.Text = "<";
            button_indietro.UseVisualStyleBackColor = true;
            button_indietro.Click += button_indietro_Click;
            // 
            // button_avanti
            // 
            button_avanti.Enabled = false;
            button_avanti.Location = new Point(224, 225);
            button_avanti.Margin = new Padding(2);
            button_avanti.Name = "button_avanti";
            button_avanti.Size = new Size(72, 36);
            button_avanti.TabIndex = 7;
            button_avanti.Text = ">";
            button_avanti.UseVisualStyleBackColor = true;
            button_avanti.Click += button_avanti_Click;
            // 
            // FormINSERISCI
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(436, 589);
            Controls.Add(button_avanti);
            Controls.Add(button_indietro);
            Controls.Add(button_random);
            Controls.Add(button_cerca);
            Controls.Add(comboBox_versettoFine);
            Controls.Add(label1);
            Controls.Add(comboBoxLibri);
            Controls.Add(comboBox_versettoDa);
            Controls.Add(label_virgola);
            Controls.Add(comboBox_capitolo);
            Controls.Add(label_capVers);
            Controls.Add(button_manutenzione);
            Controls.Add(button_leggi);
            Controls.Add(button_inserisci);
            Controls.Add(textBox_nota);
            Controls.Add(label_libro);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(2);
            MaximizeBox = false;
            Name = "FormINSERISCI";
            Text = "Emmaus 2.0";
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private Label label_libro;
        private TextBox textBox_nota;
        private Button button_inserisci;
        private Button button_leggi;
        private Button button_manutenzione;
        private ComboBox comboBox_capitolo;
        private Label label_capVers;
        private ComboBox comboBox_versettoDa;
        private Label label_virgola;
        private ComboBox comboBoxLibri;
        private ComboBox comboBox_versettoFine;
        private Label label1;
        private Button button_cerca;
        private Button button_random;
        private Button button_indietro;
        private Button button_avanti;
    }
}
