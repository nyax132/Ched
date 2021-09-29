
namespace Ched.UI
{
    partial class VolumeChange
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.Enterbutton = new System.Windows.Forms.Button();
            this.clapnum = new System.Windows.Forms.TextBox();
            this.musicnum = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(57, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(28, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "Clap";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "MusicVolume";
            // 
            // Enterbutton
            // 
            this.Enterbutton.Location = new System.Drawing.Point(251, 34);
            this.Enterbutton.Name = "Enterbutton";
            this.Enterbutton.Size = new System.Drawing.Size(75, 23);
            this.Enterbutton.TabIndex = 4;
            this.Enterbutton.Text = "OK";
            this.Enterbutton.UseVisualStyleBackColor = true;
            this.Enterbutton.Click += new System.EventHandler(this.Enterbutton_Click);
            // 
            // clapnum
            // 
            this.clapnum.Location = new System.Drawing.Point(99, 9);
            this.clapnum.Name = "clapnum";
            this.clapnum.Size = new System.Drawing.Size(100, 19);
            this.clapnum.TabIndex = 5;
            // 
            // musicnum
            // 
            this.musicnum.Location = new System.Drawing.Point(99, 36);
            this.musicnum.Name = "musicnum";
            this.musicnum.Size = new System.Drawing.Size(100, 19);
            this.musicnum.TabIndex = 6;
            // 
            // VolumeChange
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(338, 61);
            this.Controls.Add(this.musicnum);
            this.Controls.Add(this.clapnum);
            this.Controls.Add(this.Enterbutton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VolumeChange";
            this.Text = "VolumeChange";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button Enterbutton;
        private System.Windows.Forms.TextBox clapnum;
        private System.Windows.Forms.TextBox musicnum;
    }
}