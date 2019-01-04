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

namespace 破岩UI_ForHY
{
    public partial class Form1 : Form
    {

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

            for (int i = 0; i < 18; i++)
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



        private void panel6_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, this.panel6.ClientRectangle,
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

                }
                else
                {
                    MyLog.Error("单元测试仪未连接！");
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
                        this.textBox_speed.BeginInvoke(new Action(() =>
                        {
                            double speed = tempMB / tempTime;
                            textBox_speed.Text = speed.ToString();
                            this.progressBar1.Value = (int)speed;
                        }));
                    }
                }
            }
            endDT = DateTime.Now;

            this.textBox_time.BeginInvoke(
                new Action(() =>
                {
                    double costTime = endDT.Subtract(startDT).TotalSeconds;
                    double RecvdM = RecvdMB / 1024;
                    textBox_time.Text = costTime.ToString();
                    textBox_recvsize.Text = RecvdM.ToString();
                    textBox_avspeed.Text = (RecvdM / costTime).ToString();
                }));

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

                        SaveFile.Lock_7.EnterWriteLock();
                        SaveFile.DataQueue_SC7.Enqueue(buf1D0x);
                        SaveFile.Lock_7.ExitWriteLock();

                        //    string temp = null;
                        //    for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");


                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x01)//对应第1个圆盘
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);

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

                        lock (Data.SERList03)
                        {
                            Data.SERList03.AddRange(buf1D0x);
                        }

                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x0D)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);

                        //SaveFile.Lock_13.EnterWriteLock();
                        //SaveFile.DataQueue_SC13.Enqueue(buf1D0x);
                        //SaveFile.Lock_13.ExitWriteLock();

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
                        //SaveFile.Lock_14.EnterWriteLock();
                        //SaveFile.DataQueue_SC14.Enqueue(buf1D0x);
                        //SaveFile.Lock_14.ExitWriteLock();
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

                if (Tag1 == false && Tag2 == false && Tag3 == false)
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
                USB.SendData(Data.OnlyId, StrToHexByte("1D01000a" + "04008103000000029A28" + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE"));

                USB.SendData(Data.OnlyId, StrToHexByte("1D020004" + "00304591" + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE"));

                USB.SendData(Data.OnlyId, StrToHexByte("1D020004" + "00314431" + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE"));

                USB.SendData(Data.OnlyId, StrToHexByte("1D030004" + "00304591" + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE"));

                USB.SendData(Data.OnlyId, StrToHexByte("1D030004" + "00314431" + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE"));

                this.aquaGauge1.Value = Data.SER1_speed_value;

                this.aquaGauge2.Value = Data.SER2_speed_value;

                this.aquaGauge3.Value = Data.SER3_speed_value;


                for (int i = 0; i < 8; i++)
                {
                    Data.dt_AD01.Rows[i]["测量值"] = Data.daRe_AD01[i];

                    Data.dt_AD01.Rows[i + 8]["测量值"] = Data.daRe_AD02[i];

                    Data.MyPane.CurveList[i].AddPoint(Data.PaneCount, Data.daRe_AD01[i]);
                    Data.MyPane.CurveList[i + 8].AddPoint(Data.PaneCount, Data.daRe_AD02[i]);
                }

                Data.dt_AD01.Rows[16]["测量值"] = Data.SER2_niuju_value;
                Data.dt_AD01.Rows[17]["测量值"] = Data.SER3_niuju_value;

                Data.PaneCount++;


            }
        }
    }
}
