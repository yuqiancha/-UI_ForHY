namespace 破岩UI_ForHY
{
    partial class Monitor
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
            this.textBox_time = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.textBox_recvsize = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.textBox_avspeed = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.textBox_speed = new System.Windows.Forms.TextBox();
            this.label25 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // textBox_time
            // 
            this.textBox_time.Location = new System.Drawing.Point(69, 19);
            this.textBox_time.Name = "textBox_time";
            this.textBox_time.Size = new System.Drawing.Size(76, 21);
            this.textBox_time.TabIndex = 20;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(12, 22);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(53, 12);
            this.label19.TabIndex = 24;
            this.label19.Text = "总计时间";
            // 
            // textBox_recvsize
            // 
            this.textBox_recvsize.Location = new System.Drawing.Point(215, 19);
            this.textBox_recvsize.Name = "textBox_recvsize";
            this.textBox_recvsize.Size = new System.Drawing.Size(75, 21);
            this.textBox_recvsize.TabIndex = 21;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(156, 22);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(53, 12);
            this.label20.TabIndex = 25;
            this.label20.Text = "传输大小";
            // 
            // textBox_avspeed
            // 
            this.textBox_avspeed.Location = new System.Drawing.Point(70, 46);
            this.textBox_avspeed.Name = "textBox_avspeed";
            this.textBox_avspeed.Size = new System.Drawing.Size(75, 21);
            this.textBox_avspeed.TabIndex = 22;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(12, 49);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(53, 12);
            this.label21.TabIndex = 26;
            this.label21.Text = "平均速度";
            // 
            // textBox_speed
            // 
            this.textBox_speed.Location = new System.Drawing.Point(215, 46);
            this.textBox_speed.Name = "textBox_speed";
            this.textBox_speed.Size = new System.Drawing.Size(75, 21);
            this.textBox_speed.TabIndex = 23;
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(156, 49);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(53, 12);
            this.label25.TabIndex = 27;
            this.label25.Text = "瞬间速度";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(14, 73);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(276, 23);
            this.progressBar1.TabIndex = 19;
            // 
            // Monitor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(313, 118);
            this.Controls.Add(this.textBox_time);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.textBox_recvsize);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.textBox_avspeed);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.textBox_speed);
            this.Controls.Add(this.label25);
            this.Controls.Add(this.progressBar1);
            this.Name = "Monitor";
            this.Text = "Monitor";
            this.Load += new System.EventHandler(this.Monitor_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label25;
        public System.Windows.Forms.TextBox textBox_time;
        public System.Windows.Forms.TextBox textBox_recvsize;
        public System.Windows.Forms.TextBox textBox_avspeed;
        public System.Windows.Forms.TextBox textBox_speed;
        public System.Windows.Forms.ProgressBar progressBar1;
    }
}