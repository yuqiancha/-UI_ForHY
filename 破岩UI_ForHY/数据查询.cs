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

        Button[] myBtnGp;

        public GraphPane MyPane;
        public GraphPane MyPane2;
        public 数据查询()
        {
            InitializeComponent();

            myBtnGp = new Button[16] { button1, button2, button3, button4, button5, button6, button7, button8
            , button9, button10, button11, button12, button13, button14, button15, button16};

            for(int i=0;i<16;i++)
            {
                myBtnGp[i].Text = Function.GetConfigStr(Data.ADconfigPath, "add", "AD_Channel_" + i.ToString(), "name");
            }

            MyPane = zedGraphControl1.GraphPane;
            MyPane.Title.Text = "AD显示表";
            MyPane.Title.IsVisible = false;
            MyPane.Legend.Position = LegendPos.TopFlushLeft;
    

            MyPane2 = zedGraphControl2.GraphPane;
            MyPane2.Title.Text = "AD显示表2";
            MyPane2.Title.IsVisible = false;
            MyPane2.Legend.Position = LegendPos.TopFlushLeft;
  
            zedGraphControl2.IsEnableVZoom = false;
            zedGraphControl1.IsEnableVZoom = false;


        }

     
        private void 数据查询_Load(object sender, EventArgs e)
        {
            MyPane.YAxisList.Clear();
            MyPane.CurveList.Clear();

            MyPane2.YAxisList.Clear();
            MyPane2.CurveList.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string name = btn.Name;
            int t = int.Parse(name.Substring(6));//t范围（1，16）

            openFileDialog1.InitialDirectory = Program.GetStartupPath()+ @"单元测试仪数据\AD\"+btn.Text+@"\";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                try
                {
                    byte[] bufret = System.IO.File.ReadAllBytes(openFileDialog1.FileName);
                    int num = bufret.Count();
                    double yMax = 0;
                    double yMin = 0;
          
                    double[] x = new double[num/2];
                    double[] y = new double[num/2];
                    for (int i = 0; i < num/2; i++)
                    {
                        x[i] = i;
  
                        int temp = bufret[2 * i] * 256 + bufret[2 * i + 1];

                        if ((bufret[2 * i] & 0x80) == 0x80)
                        {
                            temp = 0x8000 - temp;
                        }

                        double value = temp;
                        value = 10 * (value / 32767);
                        if ((bufret[2 * i] & 0x80) == 0x80)
                            y[i] = -value;
                        else
                            y[i] = value;

                        if (i==0)
                        {                      
                            yMax = y[i];
                            yMin = y[i];
                        }
                        else
                        {
                            if (y[i] > yMax) yMax = y[i];
                            if (y[i] < yMin) yMin = y[i];
                        }
                    }

                    //加速度以及保留在zed2，其他在zed1
                    if ((t >= 2 && t <= 8) || t == 16)
                    {//zed2
                        Color color = GetColor(t);
                        LineItem myCurve = MyPane2.AddCurve(btn.Text, x, y, color, SymbolType.Square);

                        YAxis yAxis = new YAxis(btn.Text);                                               
                        yAxis.Scale.Max = yMax;
                        yAxis.Scale.Min = yMin;
                        MyPane2.YAxisList.Add(yAxis);

                        zedGraphControl2.AxisChange();
                        zedGraphControl2.Invalidate();
                    }
                    else
                    {//zed1

                        Color color = GetColor(t); 
                        LineItem myCurve = MyPane.AddCurve(btn.Text, x, y, color, SymbolType.Square);

                        YAxis yAxis = new YAxis(btn.Text);               

                        yAxis.Scale.Max = yMax;
                        yAxis.Scale.Min = yMin;
                        MyPane.YAxisList.Add(yAxis);

                        zedGraphControl1.AxisChange();
                        zedGraphControl1.Invalidate();
                    }


                }
                catch(Exception ex)
                {
                    MyLog.Error(ex.ToString());             
                }
            }
        }

        private Color GetColor(int key)
        {
            Color[] colorlist = new Color[] { Color.White,
                Color.Red, Color.Gold, Color.Green, Color.Blue,
            Color.Red, Color.DarkGray, Color.DarkGoldenrod, Color.DarkOrange,
            Color.Black, Color.Gold, Color.Green, Color.Blue,
            Color.DarkGray, Color.DarkGoldenrod, Color.DarkOrange, Color.Black};
            

            return colorlist[key];
        }
    }
}
