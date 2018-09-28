using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using log4net;
using log4net.Config;
using System.Reflection;
using System.Diagnostics;

namespace 破岩UI_ForHY
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            XmlConfigurator.Configure();
            Type type = MethodBase.GetCurrentMethod().DeclaringType;
            ILog m_log = LogManager.GetLogger(type);
            m_log.Debug("这是一个Debug日志");
            m_log.Info("这是一个Info日志");
            m_log.Warn("这是一个Warn日志");
            m_log.Error("这是一个Error日志");
            m_log.Fatal("这是一个Fatal日志");


        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
        


        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, this.panel2.ClientRectangle,
                                   Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                                   Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                                   Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                                   Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid);
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, this.panel3.ClientRectangle,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid);
        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, this.panel4.ClientRectangle,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, this.panel1.ClientRectangle,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid);
        }

        private void panel5_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, this.panel5.ClientRectangle,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid);
        }

        private void panel6_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, this.panel6.ClientRectangle,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid);
        }

        private void panel7_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, this.panel7.ClientRectangle,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
                       Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid);
        }

        private void panel8_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, this.panel8.ClientRectangle,
           Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
           Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
           Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid,
           Color.DarkSeaGreen, 2, ButtonBorderStyle.Solid);
        }
    }
}
