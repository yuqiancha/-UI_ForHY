using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;

namespace 破岩UI_ForHY
{
    public partial class 数据查询 : Form
    {
        public 数据查询()
        {
            InitializeComponent();
        }

        private void 数据查询_Load(object sender, EventArgs e)
        {
            //var text = new TextObj("On X Axis", 0.02, 1.03, CoordType.ChartFraction,
            //           AlignH.Left, AlignV.Top);
            //text.ZOrder = ZOrder.D_BehindAxis;
            //zedGraphControl1 .GraphPane.GraphObjList.Add(text);

            var text2 = new TextObj("On Y Axis", 5, 1.03, CoordType.ChartFraction,
                 AlignH.Left, AlignV.Top);
            text2.ZOrder = ZOrder.D_BehindAxis;
            
            zedGraphControl1.GraphPane.GraphObjList.Add(text2);
        }
    }
}
