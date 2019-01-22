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
using CyUSB;
using System.Threading;
using System.Configuration;
using ZedGraph;

namespace 破岩UI_ForHY
{
    public partial class Form1 : Form
    {
        public bool Chart60Tag = true;

        public 数据查询 myQueryForm;

        public Monitor myMonitor;
        SaveFile FileThread = null;
        public byte[] TempStoreBuf = new byte[8192];
        public int TempStoreBufTag = 0;

        bool RecvTag = false;
        int ThisCount = 0;
        int LastCount = 0;

        public DateTime startDT;
        public DateTime endDT;
        public int RecvdMB = 0;

        public static Queue<byte> DataQueue_1D0E = new Queue<byte>();   //处理FF08异步数传通道的数据
        public static ReaderWriterLockSlim Lock_1D0E = new ReaderWriterLockSlim();

        public static Queue<byte> DataQueue_1D0F = new Queue<byte>();   //处理FF08异步数传通道的数据
        public static ReaderWriterLockSlim Lock_1D0F = new ReaderWriterLockSlim();


        public DataTable dt_AD = new DataTable();

        public Form1()
        {
            InitializeComponent();

            //启动日志
            MyLog.richTextBox1 = richTextBox1;
            MyLog.path = Program.GetStartupPath() + @"LogData\";
            MyLog.lines = 50;
            MyLog.start();


            // Create the list of USB devices attached to the CyUSB3.sys driver.
            USB.usbDevices = new USBDeviceList(CyConst.DEVICES_CYUSB);

            //Assign event handlers for device attachment and device removal.
            USB.usbDevices.DeviceAttached += new EventHandler(UsbDevices_DeviceAttached);
            USB.usbDevices.DeviceRemoved += new EventHandler(UsbDevices_DeviceRemoved);

            USB.Init();
        }

        void UsbDevices_DeviceAttached(object sender, EventArgs e)
        {
            SetDevice(false);
        }

        void UsbDevices_DeviceRemoved(object sender, EventArgs e)
        {
            USBEventArgs evt = (USBEventArgs)e;
            USBDevice RemovedDevice = evt.Device;

            string RemovedDeviceName = evt.FriendlyName;
            MyLog.Error(RemovedDeviceName + "板卡断开");

            int key = int.Parse(evt.ProductID.ToString("x4").Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            USB.MyDeviceList[key] = null;

        }

        private void SetDevice(bool bPreserveSelectedDevice)
        {
            int nDeviceList = USB.usbDevices.Count;
            for (int nCount = 0; nCount < nDeviceList; nCount++)
            {
                USBDevice fxDevice = USB.usbDevices[nCount];
                String strmsg;
                strmsg = "(0x" + fxDevice.VendorID.ToString("X4") + " - 0x" + fxDevice.ProductID.ToString("X4") + ") " + fxDevice.FriendlyName;

                int key = int.Parse(fxDevice.ProductID.ToString("x4").Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                if (USB.MyDeviceList[key] == null)
                {
                    USB.MyDeviceList[key] = (CyUSBDevice)fxDevice;

                    MyLog.Info(USB.MyDeviceList[key].FriendlyName + ConfigurationManager.AppSettings[USB.MyDeviceList[key].FriendlyName] + "连接");

                    Data.OnlyId = key;
                }
            }

        }



        private void Form1_Load(object sender, EventArgs e)
        {
            myQueryForm = new 数据查询();
            myMonitor = new Monitor();
            XmlConfigurator.Configure();
            Type type = MethodBase.GetCurrentMethod().DeclaringType;
            ILog m_log = LogManager.GetLogger(type);
            m_log.Debug("这是一个Debug日志");
            m_log.Info("这是一个Info日志");
            m_log.Warn("这是一个Warn日志");
            m_log.Error("这是一个Error日志");
            m_log.Fatal("这是一个Fatal日志");

            dt_AD.Columns.Add("序号", typeof(Int32));
            dt_AD.Columns.Add("名称", typeof(String));
            dt_AD.Columns.Add("测量值", typeof(double));
            dt_AD.Columns.Add("解析值", typeof(double));
            dt_AD.Columns.Add("单位", typeof(String));

            for (int i = 0; i < 19; i++)
            {
                DataRow dr = dt_AD.NewRow();
                dr["序号"] = i + 1;
                dr["名称"] = Function.GetConfigStr(Data.ADconfigPath, "add", "AD_Channel_" + i.ToString(), "name");
                dr["测量值"] = 0;
                dr["解析值"] = 0;
                dr["单位"] = Function.GetConfigStr(Data.ADconfigPath, "add", "AD_Channel_" + i.ToString(), "dw");
                dt_AD.Rows.Add(dr);
            }

            dataGridView1.DataSource = dt_AD;
            dataGridView1.AllowUserToAddRows = false;

            SetDevice(false);

            Data.MyPane = zedGraphControl1.GraphPane;

            Data.MyPane.Title.Text = "AD显示表";

            Data.MyPane.XAxis.Title.Text = "时间";

            Data.MyPane.YAxis.Title.Text = "AD1";
            //      Data.MyPane.YAxis.Title.FontSpec.Size = 8;
            Data.MyPane.YAxis.Title.IsVisible = false;

            Data.MyPane.Legend.Position = LegendPos.TopFlushLeft;
            Data.MyPane.Legend.FontSpec.Size = 8;



            double[] x = new double[100];
            double[] y = new double[100];
            for (int i = 0; i < 1; i++)
            {
                x[i] = 0;
                y[i] = 0;
            }

            if (Data.MyPane.CurveList != null)
                Data.MyPane.CurveList.Clear();

            LineItem myCurve = Data.MyPane.AddCurve("AD1", x, y, Color.Red, SymbolType.Square);
            //   myCurve.Symbol.Fill = new Fill(Color.White);

            myCurve = Data.MyPane.AddCurve("AD2", x, y, Color.Gold, SymbolType.Square);
          //  myCurve.Symbol.Fill = new Fill(Color.White);
        //    myCurve.YAxisIndex = 1;

            myCurve = Data.MyPane.AddCurve("AD3", x, y, Color.Green, SymbolType.Square);
        //    myCurve.Symbol.Fill = new Fill(Color.White);
        //    myCurve.YAxisIndex = 3;

            Data.MyPane.AddCurve("AD4", x, y, Color.Blue, SymbolType.Square);
           // myCurve.Symbol.Fill = new Fill(Color.White);
          //  myCurve.YAxisIndex = 2;


            Data.MyPane.AddCurve("AD5", x, y, Color.Red, SymbolType.Square);
            Data.MyPane.AddCurve("AD6", x, y, Color.Gold, SymbolType.Square);
            Data.MyPane.AddCurve("AD7", x, y, Color.Green, SymbolType.Square);
            Data.MyPane.AddCurve("AD8", x, y, Color.Blue, SymbolType.Square);
            Data.MyPane.AddCurve("AD9", x, y, Color.Red, SymbolType.Square);
            Data.MyPane.AddCurve("AD10", x, y, Color.Gold, SymbolType.Square);
            Data.MyPane.AddCurve("AD11", x, y, Color.Green, SymbolType.Square);
            Data.MyPane.AddCurve("AD12", x, y, Color.Blue, SymbolType.Square);
            Data.MyPane.AddCurve("AD13", x, y, Color.Red, SymbolType.Square);
            Data.MyPane.AddCurve("AD14", x, y, Color.Gold, SymbolType.Square);
            Data.MyPane.AddCurve("AD15", x, y, Color.Green, SymbolType.Square);
            Data.MyPane.AddCurve("AD16", x, y, Color.Blue, SymbolType.Square);





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


        private void button7_Click(object sender, EventArgs e)
        {
            Register.Byte81H = (byte)(Register.Byte81H | 0x01);//JDQ1打开
            Register.Byte81H = (byte)(Register.Byte81H & 0x7d);//JDQ2关闭
            USB.SendCMD(Data.OnlyId, 0x81, Register.Byte81H);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Register.Byte81H = (byte)(Register.Byte81H | 0x02);//JDQ2打开
            Register.Byte81H = (byte)(Register.Byte81H & 0x7e);//JDQ1关闭
            USB.SendCMD(Data.OnlyId, 0x81, Register.Byte81H);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Register.Byte81H = (byte)(Register.Byte81H & 0x7c);//JDQ1,2关闭
            USB.SendCMD(Data.OnlyId, 0x81, Register.Byte81H);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            Register.Byte81H = (byte)(Register.Byte81H | 0x04);//JDQ3打开
            USB.SendCMD(Data.OnlyId, 0x81, Register.Byte81H);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            Register.Byte81H = (byte)(Register.Byte81H & 0x7b);//JDQ3关闭
            USB.SendCMD(Data.OnlyId, 0x81, Register.Byte81H);
        }

        private void button21_Click(object sender, EventArgs e)
        {
            Register.Byte81H = (byte)(Register.Byte81H | 0x08);//JDQ3打开
            USB.SendCMD(Data.OnlyId, 0x81, Register.Byte81H);
        }

        private void button24_Click(object sender, EventArgs e)
        {
            Register.Byte81H = (byte)(Register.Byte81H & 0x77);//JDQ3关闭
            USB.SendCMD(Data.OnlyId, 0x81, Register.Byte81H);
        }

        private static byte[] StrToHexByte(string hexString)
        {

            hexString = hexString.Replace(" ", "").Replace("\r", "").Replace("\n", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";

            byte[] returnBytes = new byte[hexString.Length / 2];

            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;

        }

        private void button10_Click(object sender, EventArgs e)
        {
            Register.Byte83H = (byte)(Register.Byte83H | 0x01);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            Register.Byte83H = (byte)(Register.Byte83H & 0x7e);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            double V = (double)numericUpDown1.Value;

            int mazi = (int)((V * 4095) / 10);
            string value = mazi.ToString("x4");
            String Str_Content = "01 06 00 00 " + value.Substring(0, 2) + " " + value.Substring(2, 2);
            int lenth = (Str_Content.Length) / 2 + 2;
            if (lenth >= 0)
            {
                string crc = Data.CRCCalc(Str_Content).Replace(" ", "").PadLeft(4, '0');
                byte[] temp = StrToHexByte("1D00" + lenth.ToString("x4") + Str_Content + crc + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE");

                USB.SendData(Data.OnlyId, temp);
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            Register.Byte83H = (byte)(Register.Byte83H | 0x01);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            Register.Byte83H = (byte)(Register.Byte83H & 0x7e);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            double V = (double)numericUpDown2.Value;

            int mazi = (int)((V * 4095) / 10);
            string value = mazi.ToString("x4");
            String Str_Content = "01 06 00 01 " + value.Substring(0, 2) + " " + value.Substring(2, 2);
            int lenth = (Str_Content.Length) / 2 + 2;
            if (lenth >= 0)
            {
                string crc = Data.CRCCalc(Str_Content).Replace(" ", "").PadLeft(4, '0');
                byte[] temp = StrToHexByte("1D00" + lenth.ToString("x4") + Str_Content + crc + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE");

                USB.SendData(Data.OnlyId, temp);
            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            Register.Byte83H = (byte)(Register.Byte83H | 0x01);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            Register.Byte83H = (byte)(Register.Byte83H & 0x7e);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            double V = (double)numericUpDown3.Value;

            int mazi = (int)((V * 4095) / 10);
            string value = mazi.ToString("x4");
            String Str_Content = "01 06 00 04 " + value.Substring(0, 2) + " " + value.Substring(2, 2);
            int lenth = (Str_Content.Length) / 2 + 2;
            if (lenth >= 0)
            {
                string crc = Data.CRCCalc(Str_Content).Replace(" ", "").PadLeft(4, '0');
                byte[] temp = StrToHexByte("1D00" + lenth.ToString("x4") + Str_Content + crc + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE");

                USB.SendData(Data.OnlyId, temp);
            }
        }

        private void button27_Click(object sender, EventArgs e)
        {
            Register.Byte83H = (byte)(Register.Byte83H | 0x01);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            Register.Byte83H = (byte)(Register.Byte83H & 0x7e);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            double V = (double)numericUpDown4.Value;

            int mazi = (int)((V * 4095) / 10);
            string value = mazi.ToString("x4");
            String Str_Content = "01 06 00 02 " + value.Substring(0, 2) + " " + value.Substring(2, 2);
            int lenth = (Str_Content.Length) / 2 + 2;
            if (lenth >= 0)
            {
                string crc = Data.CRCCalc(Str_Content).Replace(" ", "").PadLeft(4, '0');
                byte[] temp = StrToHexByte("1D00" + lenth.ToString("x4") + Str_Content + crc + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE");

                USB.SendData(Data.OnlyId, temp);
            }
        }

        private void button28_Click(object sender, EventArgs e)
        {
            Register.Byte83H = (byte)(Register.Byte83H | 0x01);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            Register.Byte83H = (byte)(Register.Byte83H & 0x7e);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            double V = (double)numericUpDown5.Value;

            int mazi = (int)((V * 4095) / 10);
            string value = mazi.ToString("x4");
            String Str_Content = "01 06 00 03 " + value.Substring(0, 2) + " " + value.Substring(2, 2);
            int lenth = (Str_Content.Length) / 2 + 2;
            if (lenth >= 0)
            {
                string crc = Data.CRCCalc(Str_Content).Replace(" ", "").PadLeft(4, '0');
                byte[] temp = StrToHexByte("1D00" + lenth.ToString("x4") + Str_Content + crc + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE");

                USB.SendData(Data.OnlyId, temp);
            }
        }


        private void 开始ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.开始ToolStripMenuItem.Text == "开始")
            {
                this.开始ToolStripMenuItem.Text = "停止";

                if (USB.MyDeviceList[Data.OnlyId] != null)
                {

                    CyControlEndPoint CtrlEndPt = null;
                    CtrlEndPt = USB.MyDeviceList[Data.OnlyId].ControlEndPt;

                    if (CtrlEndPt != null)
                    {
                        USB.SendCMD(Data.OnlyId, 0x80, 0x01);
                        USB.SendCMD(Data.OnlyId, 0x80, 0x00);

                        USB.MyDeviceList[Data.OnlyId].Reset();

                        Register.Byte80H = (byte)(Register.Byte80H | 0x04);
                        USB.SendCMD(Data.OnlyId, 0x80, Register.Byte80H);

                    }

                    FileThread = new SaveFile();
                    FileThread.FileInit();
                    FileThread.FileSaveStart();

                    MyLog.Info("开始读取");
                    RecvTag = true;

                    ThisCount = 0;
                    LastCount = 0;

                    new Thread(() => { RecvAllUSB(); }).Start();
                    new Thread(() => { DealWithADFun(); }).Start();
                    new Thread(() => { DealWithSERFun(); }).Start();
                }
                else
                {
                    MyLog.Error("设备未连接，请检查接线，打开电源！");
                }

            }
            else
            {
                this.开始ToolStripMenuItem.Text = "开始";
                ThisCount = 0;
                LastCount = 0;
                RecvTag = false;
                Thread.Sleep(500);
                if (FileThread != null)
                    FileThread.FileClose();
            }
        }

        int Recv4KCounts = 0;
        private void RecvAllUSB()
        {
            CyUSBDevice MyDevice01 = USB.MyDeviceList[Data.OnlyId];

            startDT = DateTime.Now;
            DateTime midDT = startDT;
            RecvdMB = 0;
            TempStoreBufTag = 0;
            while (RecvTag)
            {
                if (MyDevice01.BulkInEndPt != null)
                {
                    byte[] buf = new byte[4096];
                    int buflen = 4096;

                    lock (MyDevice01)
                        MyDevice01.BulkInEndPt.XferData(ref buf, ref buflen);

                    if (buflen > 0)
                    {
                        Trace.WriteLine("收到数据包长度为：" + buflen.ToString());
                        //    lock (TempStoreBuf)
                        Array.Copy(buf, 0, TempStoreBuf, TempStoreBufTag, buflen);
                        TempStoreBufTag += buflen;

                        byte[] Svbuf = new byte[buflen];
                        Array.Copy(buf, Svbuf, buflen);

                        SaveFile.Lock_1.EnterWriteLock();
                        SaveFile.DataQueue_SC1.Enqueue(Svbuf);
                        SaveFile.Lock_1.ExitWriteLock();

                        while (TempStoreBufTag >= 4096)
                        {
                            if (TempStoreBuf[0] == 0xff && (0x0 <= TempStoreBuf[1]) && (TempStoreBuf[1] < 0x11))
                            {
                                DealWithLongFrame(ref TempStoreBuf, ref TempStoreBufTag);
                            }
                            else
                            {
                                MyLog.Error("收到异常帧！");
                                Trace.WriteLine("收到异常帧" + TempStoreBufTag.ToString());
                                //       lock(TempStoreBuf)
                                Array.Clear(TempStoreBuf, 0, TempStoreBufTag);
                                TempStoreBufTag = 0;
                            }
                        }
                    }
                    else if (buflen == 0)
                    {
                        //   Trace.WriteLine("数传422机箱 收到0包-----0000000000");
                    }
                    else
                    {
                        Trace.WriteLine("收到buflen <0");
                    }

                    endDT = DateTime.Now;
                    double tempTime = endDT.Subtract(midDT).TotalSeconds;
                    if (tempTime > 2)
                    {
                        midDT = endDT;
                        double tempMB = Recv4KCounts / 256;
                        Recv4KCounts = 0;
                        //myMonitor.textBox_speed.BeginInvoke(new Action(() =>
                        //{
                        //    double speed = tempMB / tempTime;
                        //    myMonitor.textBox_speed.Text = speed.ToString();
                        //    myMonitor.progressBar1.Value = (int)speed;
                        //}));
                    }
                }
            }
            endDT = DateTime.Now;

            //myMonitor.textBox_time.BeginInvoke(
            //    new Action(() =>
            //    {
            //        double costTime = endDT.Subtract(startDT).TotalSeconds;
            //        double RecvdM = RecvdMB / 1024;
            //        myMonitor.textBox_time.Text = costTime.ToString();
            //        myMonitor.textBox_recvsize.Text = RecvdM.ToString();
            //        myMonitor.textBox_avspeed.Text = (RecvdM / costTime).ToString();
            //    }));

        }

        void DealWithLongFrame(ref byte[] TempBuf, ref int TempTag)
        {
            ThisCount = TempStoreBuf[2] * 256 + TempStoreBuf[3];
            if (LastCount != 0 && ThisCount != 0 && (ThisCount - LastCount != 1))
            {
                MyLog.Error("出现漏帧情况！！");
                Trace.WriteLine("出现漏帧情况:" + LastCount.ToString("x4") + "--" + ThisCount.ToString("x4"));
            }
            LastCount = ThisCount;

            byte[] buf_LongFrame = new byte[4096];
            Array.Copy(TempStoreBuf, 0, buf_LongFrame, 0, 4096);

            Array.Copy(TempStoreBuf, 4096, TempStoreBuf, 0, TempStoreBufTag - 4096);
            TempStoreBufTag -= 4096;

            RecvdMB += 4;
            Recv4KCounts += 1;

            if (buf_LongFrame[0] == 0xff && buf_LongFrame[1] == 0x08)
            {
                //FF08为短帧通道
                byte[] bufsav = new byte[4092];
                Array.Copy(buf_LongFrame, 4, bufsav, 0, 4092);

                //SaveFile.Lock_2.EnterWriteLock();
                //SaveFile.DataQueue_SC2.Enqueue(bufsav);
                //SaveFile.Lock_2.ExitWriteLock();

                for (int i = 0; i < 6; i++)
                {
                    if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x00)//保留
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);

                        //SaveFile.Lock_3.EnterWriteLock();
                        //SaveFile.DataQueue_SC3.Enqueue(buf1D0x);
                        //SaveFile.Lock_3.ExitWriteLock();

                        //    string temp = null;
                        //    for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");


                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x01)//对应第1个圆盘
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);

                        //SaveFile.Lock_4.EnterWriteLock();
                        //SaveFile.DataQueue_SC4.Enqueue(buf1D0x);
                        //SaveFile.Lock_4.ExitWriteLock();

                        lock (Data.SERList01)
                        {
                            Data.SERList01.AddRange(buf1D0x);
                        }

                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x02)//对应第2个圆盘
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);

                        //SaveFile.Lock_5.EnterWriteLock();
                        //SaveFile.DataQueue_SC5.Enqueue(buf1D0x);
                        //SaveFile.Lock_5.ExitWriteLock();

                        lock (Data.SERList02)
                        {
                            Data.SERList02.AddRange(buf1D0x);
                        }

                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x03)//对应第3个圆盘
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);

                        //SaveFile.Lock_6.EnterWriteLock();
                        //SaveFile.DataQueue_SC6.Enqueue(buf1D0x);
                        //SaveFile.Lock_6.ExitWriteLock();

                        lock (Data.SERList03)
                        {
                            Data.SERList03.AddRange(buf1D0x);
                        }

                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x0C)//报警参数
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);

                        //SaveFile.Lock_6.EnterWriteLock();
                        //SaveFile.DataQueue_SC6.Enqueue(buf1D0x);
                        //SaveFile.Lock_6.ExitWriteLock();


                        lock (Data.SERList04)
                        {
                            Data.SERList04.AddRange(buf1D0x);
                        }


                        //myMonitor.textBox_speed.BeginInvoke(new Action(() =>
                        //{
                        //    double speed = tempMB / tempTime;
                        //    myMonitor.textBox_speed.Text = speed.ToString();
                        //    myMonitor.progressBar1.Value = (int)speed;
                        //}));

                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x0D)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);

                        //SaveFile.Lock_7.EnterWriteLock();
                        //SaveFile.DataQueue_SC7.Enqueue(buf1D0x);
                        //SaveFile.Lock_7.ExitWriteLock();

                        lock (Data.ADList01)
                        {
                            Data.ADList01.AddRange(buf1D0x);
                            //for (int j = 0; j < num; j++)
                            //    Data.ADList01.Add(buf1D0x[j]);
                        }
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x0E)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);

                        //SaveFile.Lock_8.EnterWriteLock();
                        //SaveFile.DataQueue_SC8.Enqueue(buf1D0x);
                        //SaveFile.Lock_8.ExitWriteLock();

                        lock (Data.ADList02)
                        {
                            Data.ADList02.AddRange(buf1D0x);
                            //for (int j = 0; j < num; j++)
                            //    Data.ADList02.Add(buf1D0x[j]);
                        }

                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x0f)
                    {
                        //空闲帧
                    }
                    else
                    {
                        Trace.WriteLine("FF08通道出错!");
                    }
                }
            }
        }

        private void DealWithSERFun()
        {
            while (RecvTag)
            {
                bool Tag1 = false;
                bool Tag2 = false;
                bool Tag3 = false;
                bool Tag4 = false;

                lock (Data.SERList01)
                {
                    if (Data.SERList01.Count >= 11)
                    {
                        byte[] ret = Data.SERList01.Skip(5).Take(4).ToArray();

                        int weight = ret[0] * 256 + ret[1];
                        if (Data.SERList01[0] == 0x04 && Data.SERList01[1] == 0x00 && Data.SERList01[2] == 0x01)
                        {
                            Data.SER1_speed_value = weight;
                        }
                        else
                        {
                            MyLog.Error("SERList01 串口数据偏移了！");
                        }
                        Data.SERList01.RemoveRange(0, 11);
                        Tag1 = true;
                    }
                    else
                    {
                        Tag1 = false;
                    }
                }

                lock (Data.SERList02)
                {
                    if (Data.SERList02.Count >= 9)
                    {
                        byte[] ret = Data.SERList02.Skip(3).Take(4).ToArray();
                        float temp = BitConverter.ToSingle(ret, 0);

                        if (Data.SERList02[1] == 0x31)
                        {
                            Data.SER2_speed_value = temp;
                        }
                        else if (Data.SERList02[1] == 0x30)
                        {
                            Data.SER2_niuju_value = temp;
                        }
                        else
                        {
                            MyLog.Error("SERList02 串口数据偏移了！");
                        }


                        Data.SERList02.RemoveRange(0, 9);
                        Tag2 = true;
                    }
                    else
                    {
                        Tag2 = false;
                    }
                }

                lock (Data.SERList03)
                {
                    if (Data.SERList03.Count >= 9)
                    {
                        byte[] ret = Data.SERList03.Skip(3).Take(4).ToArray();
                        float temp = BitConverter.ToSingle(ret, 0);

                        if (Data.SERList03[1] == 0x31)
                        {
                            Data.SER3_speed_value = temp;
                        }
                        else if (Data.SERList03[1] == 0x30)
                        {
                            Data.SER3_niuju_value = temp;
                        }
                        else
                        {
                            MyLog.Error("SERList03 串口数据偏移了！");
                        }

                        Data.SERList03.RemoveRange(0, 9);
                        Tag3 = true;
                    }
                    else
                    {
                        Tag3 = false;
                    }
                }

                lock(Data.SERList04)
                {
                    if (Data.SERList04.Count >= 100)
                    {
                        byte ret = Data.SERList04[0];

                        Data.SERList04.RemoveRange(0, 100);
                        Tag4 = true;
                    }
                    else
                    {
                        Tag4 = false;
                    }
                }

                if (Tag1 == false && Tag2 == false && Tag3 == false && Tag4 == false)
                {
                    Thread.Sleep(100);
                }

            }
        }

        private void DealWithADFun()
        {
            while (RecvTag)
            {
                bool Tag1 = false;
                bool Tag2 = false;

                lock (Data.ADList01)
                {
                    if (Data.ADList01.Count > 16)
                    {
                        Tag1 = true;

                        byte[] buf = new byte[16];
                        for (int t = 0; t < 16; t++)
                        {
                            buf[t] = Data.ADList01[t];
                        }


                        SaveFile.Lock_9.EnterWriteLock();
                        SaveFile.DataQueue_SC9.Enqueue(buf.Skip(0).Take(2).ToArray());
                        SaveFile.Lock_9.ExitWriteLock();

                        SaveFile.Lock_10.EnterWriteLock();
                        SaveFile.DataQueue_SC10.Enqueue(buf.Skip(2).Take(2).ToArray());
                        SaveFile.Lock_10.ExitWriteLock();

                        SaveFile.Lock_11.EnterWriteLock();
                        SaveFile.DataQueue_SC11.Enqueue(buf.Skip(4).Take(2).ToArray());
                        SaveFile.Lock_11.ExitWriteLock();

                        SaveFile.Lock_12.EnterWriteLock();
                        SaveFile.DataQueue_SC12.Enqueue(buf.Skip(6).Take(2).ToArray());
                        SaveFile.Lock_12.ExitWriteLock();

                        SaveFile.Lock_13.EnterWriteLock();
                        SaveFile.DataQueue_SC13.Enqueue(buf.Skip(8).Take(2).ToArray());
                        SaveFile.Lock_13.ExitWriteLock();

                        SaveFile.Lock_14.EnterWriteLock();
                        SaveFile.DataQueue_SC14.Enqueue(buf.Skip(10).Take(2).ToArray());
                        SaveFile.Lock_14.ExitWriteLock();

                        SaveFile.Lock_15.EnterWriteLock();
                        SaveFile.DataQueue_SC15.Enqueue(buf.Skip(12).Take(2).ToArray());
                        SaveFile.Lock_15.ExitWriteLock();

                        SaveFile.Lock_16.EnterWriteLock();
                        SaveFile.DataQueue_SC16.Enqueue(buf.Skip(14).Take(2).ToArray());
                        SaveFile.Lock_16.ExitWriteLock();

                        for (int k = 0; k < 8; k++)
                        {
                            int temp = (buf[2 * k] & 0x7f) * 256 + buf[2 * k + 1];

                            if ((buf[2 * k] & 0x80) == 0x80)
                            {
                                temp = 0x8000 - temp;
                            }

                            double value = temp;
                            value = 10 * (value / 32767);
                            if ((buf[2 * k] & 0x80) == 0x80)
                                Data.daRe_AD01[k] = -value;
                            else
                                Data.daRe_AD01[k] = value;
                        }
                        Data.ADList01.RemoveRange(0, 16);
                    }
                    else
                    {
                        Tag1 = false;
                    }


                }


                lock (Data.ADList02)
                {

                    if (Data.ADList02.Count > 16)
                    {
                        Tag2 = true;
                        byte[] buf = new byte[16];
                        for (int t = 0; t < 16; t++)
                        {
                            buf[t] = Data.ADList02[t];
                        }

                        SaveFile.Lock_17.EnterWriteLock();
                        SaveFile.DataQueue_SC17.Enqueue(buf.Skip(0).Take(2).ToArray());
                        SaveFile.Lock_17.ExitWriteLock();

                        SaveFile.Lock_18.EnterWriteLock();
                        SaveFile.DataQueue_SC18.Enqueue(buf.Skip(2).Take(2).ToArray());
                        SaveFile.Lock_18.ExitWriteLock();

                        SaveFile.Lock_19.EnterWriteLock();
                        SaveFile.DataQueue_SC19.Enqueue(buf.Skip(4).Take(2).ToArray());
                        SaveFile.Lock_19.ExitWriteLock();

                        SaveFile.Lock_20.EnterWriteLock();
                        SaveFile.DataQueue_SC20.Enqueue(buf.Skip(6).Take(2).ToArray());
                        SaveFile.Lock_20.ExitWriteLock();

                        SaveFile.Lock_21.EnterWriteLock();
                        SaveFile.DataQueue_SC21.Enqueue(buf.Skip(8).Take(2).ToArray());
                        SaveFile.Lock_21.ExitWriteLock();

                        SaveFile.Lock_22.EnterWriteLock();
                        SaveFile.DataQueue_SC22.Enqueue(buf.Skip(10).Take(2).ToArray());
                        SaveFile.Lock_22.ExitWriteLock();

                        SaveFile.Lock_23.EnterWriteLock();
                        SaveFile.DataQueue_SC23.Enqueue(buf.Skip(12).Take(2).ToArray());
                        SaveFile.Lock_23.ExitWriteLock();

                        SaveFile.Lock_24.EnterWriteLock();
                        SaveFile.DataQueue_SC24.Enqueue(buf.Skip(14).Take(2).ToArray());
                        SaveFile.Lock_24.ExitWriteLock();

                        for (int k = 0; k < 8; k++)
                        {
                            int temp = (buf[2 * k] & 0x7f) * 256 + buf[2 * k + 1];

                            if ((buf[2 * k] & 0x80) == 0x80)
                            {
                                temp = 0x8000 - temp;
                            }

                            double value = temp;
                            value = 10 * (value / 32767);
                            if ((buf[2 * k] & 0x80) == 0x80)
                                Data.daRe_AD02[k] = -value;
                            else
                                Data.daRe_AD02[k] = value;
                        }
                        Data.ADList02.RemoveRange(0, 16);
                    }
                    else
                    {
                        Tag2 = false;
                    }
                }

                if (Tag1 == false && Tag2 == false)
                {
                    Thread.Sleep(100);
                }


            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (RecvTag)
            {             

                this.aquaGauge1.Value = Data.SER1_speed_value;

                this.aquaGauge2.Value = Data.SER2_speed_value;

                this.aquaGauge3.Value = Data.SER3_speed_value;

                double LenValue = Data.daRe_AD02[0] - (double)dt_AD.Rows[8]["测量值"];
                dt_AD.Rows[18]["测量值"] = LenValue *2*60;//0.5s间隔，单位是1min
                for (int i = 0; i < 8; i++)
                {
                    dt_AD.Rows[i]["测量值"] = Data.daRe_AD01[i];

                    dt_AD.Rows[i + 8]["测量值"] = Data.daRe_AD02[i];

                    Data.MyPane.CurveList[i].AddPoint(Data.PaneCount, Data.daRe_AD01[i]);
                    Data.MyPane.CurveList[i + 8].AddPoint(Data.PaneCount, Data.daRe_AD02[i]);
                }

                dt_AD.Rows[16]["测量值"] = Data.SER2_niuju_value;
                dt_AD.Rows[17]["测量值"] = Data.SER3_niuju_value;

               

                Data.PaneCount++;

                if (Chart60Tag)
                {
                    Data.MyPane.XAxis.Scale.Max = Data.PaneCount;
                    Data.MyPane.XAxis.Scale.Min = Data.PaneCount - 60;
                }
                else
                {
                    Data.MyPane.XAxis.Scale.MaxAuto = true;
                    Data.MyPane.XAxis.Scale.Min = 0;
                }
                zedGraphControl1.AxisChange();
                zedGraphControl1.Invalidate();
            }
        }

        private void 实时速率ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (myMonitor != null)
            {
                myMonitor.Activate();
            }
            else
            {
                myMonitor = new Monitor();
            }
            myMonitor.ShowDialog();
        }

        private void 显示最近60sToolStripMenuItem_Click(object sender, EventArgs e)
        {
            显示最近60sToolStripMenuItem.Checked = true;
            全部显示ToolStripMenuItem.Checked = false;
            Chart60Tag = true;

        }

        private void 全部显示ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            全部显示ToolStripMenuItem.Checked = true;
            显示最近60sToolStripMenuItem.Checked = false;
            Chart60Tag = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            RecvTag = false;
            Thread.Sleep(500);
            Environment.Exit(0);

        }

        private void 数据查询ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (myQueryForm != null)
            {
                myQueryForm.Activate();
            }
            else
            {
                myQueryForm = new 数据查询();
            }
            myQueryForm.ShowDialog();
        }

        private void 通道1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Register.Byte83H = (byte)(Register.Byte83H | 0x02);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            Register.Byte83H = (byte)(Register.Byte83H & 0x7d);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            USB.SendData(Data.OnlyId, StrToHexByte("1D01000a" + "04008103000000029A28" + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE"));
        }

        private void 通道2ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Register.Byte83H = (byte)(Register.Byte83H | 0x04);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            Register.Byte83H = (byte)(Register.Byte83H & 0x7b);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            USB.SendData(Data.OnlyId, StrToHexByte("1D020004" + "00304591" + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE"));


            Register.Byte83H = (byte)(Register.Byte83H | 0x04);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            Register.Byte83H = (byte)(Register.Byte83H & 0x7b);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            USB.SendData(Data.OnlyId, StrToHexByte("1D020004" + "00314431" + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE"));
        }

        private void 通道3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Register.Byte83H = (byte)(Register.Byte83H | 0x08);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            Register.Byte83H = (byte)(Register.Byte83H & 0x77);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            USB.SendData(Data.OnlyId, StrToHexByte("1D030004" + "00304591" + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE"));

            Register.Byte83H = (byte)(Register.Byte83H | 0x08);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            Register.Byte83H = (byte)(Register.Byte83H & 0x77);
            USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);

            USB.SendData(Data.OnlyId, StrToHexByte("1D030004" + "00314431" + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE"));
        }
    }
}
