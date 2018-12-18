using CyUSB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ZedGraph;

namespace USBSpeedTest
{
    public partial class MainForm : Form
    {

        GraphPane MyPane;
        public ADForm myADForm;
        SaveFile FileThread = null;
        public byte[] TempStoreBuf = new byte[8192];
        public int TempStoreBufTag = 0;

        public DateTime startDT;
        public DateTime endDT;
        public int RecvdMB = 0;

        public static Queue<byte> DataQueue_1D0E = new Queue<byte>();   //处理FF08异步数传通道的数据
        public static ReaderWriterLockSlim Lock_1D0E = new ReaderWriterLockSlim();

        public static Queue<byte> DataQueue_1D0F = new Queue<byte>();   //处理FF08异步数传通道的数据
        public static ReaderWriterLockSlim Lock_1D0F = new ReaderWriterLockSlim();

        int ThisCount = 0;
        int LastCount = 0;


        private DataTable dtR1 = new DataTable();

        public MainForm()
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

        /*Summary
        This is the event handler for device removal. This method resets the device count and searches for the device with VID-PID 04b4-1003
        */
        void UsbDevices_DeviceRemoved(object sender, EventArgs e)
        {
            USBEventArgs evt = (USBEventArgs)e;
            USBDevice RemovedDevice = evt.Device;

            string RemovedDeviceName = evt.FriendlyName;
            MyLog.Error(RemovedDeviceName + "板卡断开");

            int key = int.Parse(evt.ProductID.ToString("x4").Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            USB.MyDeviceList[key] = null;

        }

        private void initRBoard()
        {
            MyLog.Info("初始化数字电阻通道......");

            //初始化板卡，每个通道值设置为0x00001C03向下发送
            byte[] RSend = new byte[8 + 32];
            RSend[0] = 0x1D;
            RSend[1] = 0x0D;
            RSend[2] = 0x00;
            RSend[3] = 0x20;//0x0020 = 32

            // for (int i = 0; i < 32; i++) RSend[164 + i] = 0x0;

            for (int i = 1; i < 8; i++)
            {
                RSend[4 * i] = 0x0;
                RSend[4 * i + 1] = 0x0;
                //RSend[4 * i + 2] = 0x1c;
                //RSend[4 * i + 3] = 0x03;
                RSend[4 * i + 2] = 0x03;
                RSend[4 * i + 3] = 0x1c;
            }

            RSend[36] = 0xC0;
            RSend[37] = 0xDE;
            RSend[38] = 0xC0;
            RSend[39] = 0xDE;

            Register.Byte84H = (byte)(Register.Byte84H | 0x40);
            USB.SendCMD(Data.OnlyId, 0x84, Register.Byte84H);
            Register.Byte84H = (byte)(Register.Byte84H & 0x3f);
            USB.SendCMD(Data.OnlyId, 0x84, 0x00);

            USB.SendData(Data.OnlyId, RSend);

        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            SetDevice(false);

            //       zedGraphControl1.GraphPane.Title = "AD显示表";
            MyPane = zedGraphControl1.GraphPane;
            MyPane.Title = "AD显示表";
            MyPane.XAxis.Title = "时间";
            MyPane.YAxis.Title = "值";
            double[] x = new double[100];
            double[] y = new double[100];
            for (int i = 0; i < 1; i++)
            {
                x[i] = 0;
                y[i] = 0;
            }

            MyPane.AddCurve("AD1", x, y, Color.Red, SymbolType.Square);
            MyPane.AddCurve("AD2", x, y, Color.Gold, SymbolType.Square);
            MyPane.AddCurve("AD3", x, y, Color.Green, SymbolType.Square);
            MyPane.AddCurve("AD4", x, y, Color.Blue, SymbolType.Square);
            MyPane.AddCurve("AD5", x, y, Color.Red, SymbolType.Square);
            MyPane.AddCurve("AD6", x, y, Color.Gold, SymbolType.Square);
            MyPane.AddCurve("AD7", x, y, Color.Green, SymbolType.Square);
            MyPane.AddCurve("AD8", x, y, Color.Blue, SymbolType.Square);
            MyPane.AddCurve("AD9", x, y, Color.Red, SymbolType.Square);
            MyPane.AddCurve("AD10", x, y, Color.Gold, SymbolType.Square);
            MyPane.AddCurve("AD11", x, y, Color.Green, SymbolType.Square);
            MyPane.AddCurve("AD12", x, y, Color.Blue, SymbolType.Square);
            MyPane.AddCurve("AD13", x, y, Color.Red, SymbolType.Square);
            MyPane.AddCurve("AD14", x, y, Color.Gold, SymbolType.Square);
            MyPane.AddCurve("AD15", x, y, Color.Green, SymbolType.Square);
            MyPane.AddCurve("AD16", x, y, Color.Blue, SymbolType.Square);

            Data.dt_AD01.Columns.Add("序号", typeof(Int32));
            Data.dt_AD01.Columns.Add("名称", typeof(String));
            Data.dt_AD01.Columns.Add("测量值", typeof(double));
            for (int i = 0; i < 8; i++)
            {
                DataRow dr = Data.dt_AD01.NewRow();
                dr["序号"] = i + 1;
                dr["名称"] = "通道" + (i + 1).ToString();
                dr["测量值"] = 0;
                Data.dt_AD01.Rows.Add(dr);
            }


            Data.dt_AD02.Columns.Add("序号", typeof(Int32));
            Data.dt_AD02.Columns.Add("名称", typeof(String));
            Data.dt_AD02.Columns.Add("测量值", typeof(double));
            for (int i = 8; i < 16; i++)
            {
                DataRow dr = Data.dt_AD02.NewRow();
                dr["序号"] = i + 1;
                dr["名称"] = "通道" + (i + 1).ToString();
                dr["测量值"] = 0;
                Data.dt_AD02.Rows.Add(dr);
            }

            myADForm = new ADForm();


            dtR1.Columns.Add("通道", typeof(Int32));
            dtR1.Columns.Add("名称", typeof(String));
            dtR1.Columns.Add("电阻", typeof(Double));
            for (int i = 0; i < 8; i++)
            {
                DataRow dr = dtR1.NewRow();
                dr["通道"] = i + 1;
                dr["名称"] = "电阻通道" + (i + 1).ToString();
                dr["电阻"] = 20;
                dtR1.Rows.Add(dr);
            }
            dataGridView_R1.DataSource = dtR1;
            dataGridView_R1.AllowUserToAddRows = false;
        }

        /*Summary
Search the device with VID-PID 04b4-00F1 and if found, select the end point
*/
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

                    initRBoard();
                }
            }

        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        bool RecvTag = false;
        private void button1_Click(object sender, EventArgs e)
        {
            if (this.button1.Text == "开始读取")
            {
                this.textBox1.Text = null;
                this.textBox2.Text = null;
                this.textBox3.Text = null;

                this.button1.Text = "停止读取";


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

                        this.btn_80_2.Text = "1:接收";

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
                this.button1.Text = "开始读取";
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

                    MyDevice01.BulkInEndPt.XferData(ref buf, ref buflen);

                    if (buflen > 0)
                    {
                        Trace.WriteLine("收到数据包长度为：" + buflen.ToString());
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
                        this.textBox4.BeginInvoke(new Action(() =>
                        {
                            double speed = tempMB / tempTime;
                            textBox4.Text = speed.ToString();
                            this.progressBar1.Value = (int)speed;
                        }));
                    }
                }
            }
            endDT = DateTime.Now;

            this.textBox1.BeginInvoke(
                new Action(() =>
                {
                    double costTime = endDT.Subtract(startDT).TotalSeconds;
                    double RecvdM = RecvdMB / 1024;
                    textBox1.Text = costTime.ToString();
                    textBox2.Text = RecvdM.ToString();
                    textBox3.Text = (RecvdM / costTime).ToString();
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
            if (buf_LongFrame[0] == 0xff && buf_LongFrame[1] == 0x01)
            {
                byte[] bufsav = new byte[4092];
                Array.Copy(buf_LongFrame, 4, bufsav, 0, 4092);
                SaveFile.Lock_3.EnterWriteLock();
                SaveFile.DataQueue_SC3.Enqueue(bufsav);
                SaveFile.Lock_3.ExitWriteLock();
            }
            if (buf_LongFrame[0] == 0xff && buf_LongFrame[1] == 0x02)
            {
                byte[] bufsav = new byte[4092];
                Array.Copy(buf_LongFrame, 4, bufsav, 0, 4092);
                SaveFile.Lock_4.EnterWriteLock();
                SaveFile.DataQueue_SC4.Enqueue(bufsav);
                SaveFile.Lock_4.ExitWriteLock();
            }
            if (buf_LongFrame[0] == 0xff && buf_LongFrame[1] == 0x03)
            {
                byte[] bufsav = new byte[4092];
                Array.Copy(buf_LongFrame, 4, bufsav, 0, 4092);
                SaveFile.Lock_5.EnterWriteLock();
                SaveFile.DataQueue_SC5.Enqueue(bufsav);
                SaveFile.Lock_5.ExitWriteLock();
            }
            if (buf_LongFrame[0] == 0xff && buf_LongFrame[1] == 0x04)
            {
                byte[] bufsav = new byte[4092];
                Array.Copy(buf_LongFrame, 4, bufsav, 0, 4092);
                SaveFile.Lock_6.EnterWriteLock();
                SaveFile.DataQueue_SC6.Enqueue(bufsav);
                SaveFile.Lock_6.ExitWriteLock();
            }

            if (buf_LongFrame[0] == 0xff && buf_LongFrame[1] == 0x08)
            {
                //FF08为短帧通道
                byte[] bufsav = new byte[4092];
                Array.Copy(buf_LongFrame, 4, bufsav, 0, 4092);
                SaveFile.Lock_2.EnterWriteLock();
                SaveFile.DataQueue_SC2.Enqueue(bufsav);
                SaveFile.Lock_2.ExitWriteLock();

                for (int i = 0; i < 6; i++)
                {
                    if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x00)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);

                        SaveFile.Lock_7.EnterWriteLock();
                        SaveFile.DataQueue_SC7.Enqueue(buf1D0x);
                        SaveFile.Lock_7.ExitWriteLock();

                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial01.AppendText(temp); }));

                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x01)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);

                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial02.AppendText(temp); }));

                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x02)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);

                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial03.AppendText(temp); }));
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x03)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);
                        SaveFile.Lock_10.EnterWriteLock();
                        SaveFile.DataQueue_SC10.Enqueue(buf1D0x);
                        SaveFile.Lock_10.ExitWriteLock();

                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial04.AppendText(temp); }));
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x04)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);
                        SaveFile.Lock_11.EnterWriteLock();
                        SaveFile.DataQueue_SC11.Enqueue(buf1D0x);
                        SaveFile.Lock_11.ExitWriteLock();

                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial05.AppendText(temp); }));
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x05)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);
                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial06.AppendText(temp); }));
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x06)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);
                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial07.AppendText(temp); }));
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x07)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);
                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial08.AppendText(temp); }));
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x08)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);
                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial09.AppendText(temp); }));
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x09)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);
                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial10.AppendText(temp); }));
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x0A)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);
                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial11.AppendText(temp); }));
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x0B)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);
                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial12.AppendText(temp); }));
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x0C)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);
                        string temp = null;
                        for (int j = 0; j < num; j++) temp += buf1D0x[j].ToString("x2");
                        this.Invoke(new Action(() => { textBox_Serial13.AppendText(temp); }));
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x0D)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);
                        SaveFile.Lock_13.EnterWriteLock();
                        SaveFile.DataQueue_SC13.Enqueue(buf1D0x);
                        SaveFile.Lock_13.ExitWriteLock();

                        lock (Data.ADList01)
                        {
                            for (int j = 0; j < num; j++)
                                Data.ADList01.Add(buf1D0x[j]);
                        }
                    }
                    else if (bufsav[i * 682 + 0] == 0x1D && bufsav[i * 682 + 1] == 0x0E)
                    {
                        int num = bufsav[i * 682 + 2] * 256 + bufsav[i * 682 + 3];//有效位
                        byte[] buf1D0x = new byte[num];
                        Array.Copy(bufsav, i * 682 + 4, buf1D0x, 0, num);
                        SaveFile.Lock_14.EnterWriteLock();
                        SaveFile.DataQueue_SC14.Enqueue(buf1D0x);
                        SaveFile.Lock_14.ExitWriteLock();
                        lock (Data.ADList02)
                        {
                            for (int j = 0; j < num; j++)
                                Data.ADList02.Add(buf1D0x[j]);
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
                    Thread.Sleep(500);
                }


            }
        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            RecvTag = false;

            if (USB.usbDevices != null)
            {
                USB.usbDevices.DeviceRemoved -= UsbDevices_DeviceRemoved;
                USB.usbDevices.DeviceAttached -= UsbDevices_DeviceAttached;
                USB.usbDevices.Dispose();
            }


            Thread.Sleep(200);
            if (FileThread != null)
                FileThread.FileClose();

            this.Dispose();



        }

        private void button2_Click(object sender, EventArgs e)
        {
            Register.Byte81H = (byte)(Register.Byte81H | 0x01);
            USB.SendCMD(Data.OnlyId, 0x81, Register.Byte81H);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            USB.SendCMD(Data.OnlyId, 0x81, 0x00);

            Register.Byte80H = (byte)(Register.Byte80H | 0x01);
            USB.SendCMD(Data.OnlyId, 0x80, Register.Byte80H);

            Register.Byte80H = (byte)(Register.Byte80H & 0xfe);
            USB.SendCMD(Data.OnlyId, 0x80, Register.Byte80H);


        }

        private void button4_Click(object sender, EventArgs e)
        {
            Register.Byte81H = (byte)(Register.Byte81H | 0x08);
            USB.SendCMD(Data.OnlyId, 0x81, Register.Byte81H);

        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                byte addr = Convert.ToByte(textBox5.Text, 16);

                byte value = Convert.ToByte(textBox6.Text, 16);

                if (addr < 0x80 || addr > 0xff)
                {
                    MessageBox.Show("请输入正确的addr!!");
                }
                else if (value < 0x00 || value > 0x7f)
                {
                    MessageBox.Show("请输入正确的value!!");
                }
                else
                {
                    USB.SendCMD(Data.OnlyId, addr, value);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("请输入正确的地址和值：" + ex.Message);
            }


        }

        private void button6_Click(object sender, EventArgs e)
        {

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

        private void button7_Click(object sender, EventArgs e)
        {
            String Str_Content = textBox7.Text.Replace(" ", "");
            int lenth = (Str_Content.Length) / 2;
            if (lenth >= 0)
            {
                int AddToFour = lenth % 4;
                if (AddToFour != 0)
                {
                    for (int i = 0; i < (4 - AddToFour); i++) Str_Content += "00";
                }

                byte[] temp = StrToHexByte(textBox8.Text + lenth.ToString("x4") + Str_Content + textBox9.Text);

                USB.SendData(Data.OnlyId, temp);
            }
            else
            {
                MyLog.Error("请至少输入4个Byte的数据");
            }
        }

        private void btn_80_0_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string str = btn.Name;

            string TextName = btn.Text;
            string[] tempList = TextName.Split(':');

            if (tempList[0] == "0")
            {

                btn.Text = "1:" + tempList[1];

                byte addr = Convert.ToByte(str.Substring(4, 2), 16);
                int bitpos = Convert.ToByte(str.Substring(7, 1), 10);
                switch (addr)
                {
                    case 0x80:
                        Register.Byte80H = (byte)(Register.Byte80H | (byte)(0x01 << bitpos));
                        USB.SendCMD(Data.OnlyId, 0x80, Register.Byte80H);
                        break;
                    case 0x81:
                        Register.Byte81H = (byte)(Register.Byte81H | (byte)(0x01 << bitpos));
                        USB.SendCMD(Data.OnlyId, 0x81, Register.Byte81H);
                        break;
                    case 0x82:
                        Register.Byte82H = (byte)(Register.Byte82H | (byte)(0x01 << bitpos));
                        USB.SendCMD(Data.OnlyId, 0x82, Register.Byte82H);
                        break;
                    case 0x83:
                        Register.Byte83H = (byte)(Register.Byte83H | (byte)(0x01 << bitpos));
                        USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);
                        break;
                    case 0x84:
                        Register.Byte84H = (byte)(Register.Byte84H | (byte)(0x01 << bitpos));
                        USB.SendCMD(Data.OnlyId, 0x84, Register.Byte84H);
                        break;
                    case 0x85:
                        Register.Byte85H = (byte)(Register.Byte85H | (byte)(0x01 << bitpos));
                        USB.SendCMD(Data.OnlyId, 0x85, Register.Byte85H);
                        break;
                    case 0x86:
                        Register.Byte86H = (byte)(Register.Byte86H | (byte)(0x01 << bitpos));
                        USB.SendCMD(Data.OnlyId, 0x86, Register.Byte86H);
                        break;
                    default:
                        Register.Byte80H = (byte)(Register.Byte80H | (byte)(0x01 << bitpos));
                        USB.SendCMD(Data.OnlyId, 0x80, Register.Byte80H);
                        break;
                }

            }
            else
            {

                btn.Text = "0:" + tempList[1];

                byte addr = Convert.ToByte(str.Substring(4, 2), 16);
                int bitpos = Convert.ToByte(str.Substring(7, 1), 10);
                switch (addr)
                {
                    case 0x80:
                        Register.Byte80H = (byte)(Register.Byte80H & (byte)(0x7f - (byte)(0x01 << bitpos)));
                        USB.SendCMD(Data.OnlyId, 0x80, Register.Byte80H);
                        break;
                    case 0x81:
                        Register.Byte81H = (byte)(Register.Byte81H & (byte)(0x7f - (byte)(0x01 << bitpos)));
                        USB.SendCMD(Data.OnlyId, 0x81, Register.Byte81H);
                        break;
                    case 0x82:
                        Register.Byte82H = (byte)(Register.Byte82H & (byte)(0x7f - (byte)(0x01 << bitpos)));
                        USB.SendCMD(Data.OnlyId, 0x82, Register.Byte82H);
                        break;
                    case 0x83:
                        Register.Byte83H = (byte)(Register.Byte83H & (byte)(0x7f - (byte)(0x01 << bitpos)));
                        USB.SendCMD(Data.OnlyId, 0x83, Register.Byte83H);
                        break;
                    case 0x84:
                        Register.Byte84H = (byte)(Register.Byte84H & (byte)(0x7f - (byte)(0x01 << bitpos)));
                        USB.SendCMD(Data.OnlyId, 0x84, Register.Byte84H);
                        break;
                    case 0x85:
                        Register.Byte85H = (byte)(Register.Byte85H & (byte)(0x7f - (byte)(0x01 << bitpos)));
                        USB.SendCMD(Data.OnlyId, 0x85, Register.Byte85H);
                        break;
                    case 0x86:
                        Register.Byte86H = (byte)(Register.Byte86H & (byte)(0x7f - (byte)(0x01 << bitpos)));
                        USB.SendCMD(Data.OnlyId, 0x86, Register.Byte86H);
                        break;
                    default:
                        Register.Byte80H = (byte)(Register.Byte80H & (byte)(0x7f - (byte)(0x01 << bitpos)));
                        USB.SendCMD(Data.OnlyId, 0x80, Register.Byte80H);
                        break;
                }

            }

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            this.richTextBox1.Clear();
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            double str_len = textBox7.Text.Length;
            double byte_len = str_len / 2;
            textBox10.Text = byte_len.ToString();
        }


        private void button3_Click_1(object sender, EventArgs e)
        {
            String Str_Content = textBox7.Text.Replace(" ", "");
            int lenth = (Str_Content.Length) / 2;
            if (lenth >= 0)
            {
                int AddToFour = lenth % 4;
                if (AddToFour != 0)
                {
                    for (int i = 0; i < (4 - AddToFour); i++) Str_Content += "00";
                }

                for (int j = 0; j < 8; j++)
                {
                    byte[] temp = StrToHexByte((0x1D00 + j).ToString("x4") + lenth.ToString("x4") + Str_Content + textBox9.Text);
                    temp[4] = (byte)(0x1 + j);
                    USB.SendData(Data.OnlyId, temp);
                }
            }
            else
            {
                MyLog.Error("请至少输入4个Byte的数据");
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            USB.SendCMD(Data.OnlyId, 0x82, 0x7f);
            Thread.Sleep(100);
            USB.SendCMD(Data.OnlyId, 0x82, 0x00);
            Thread.Sleep(100);
            USB.SendCMD(Data.OnlyId, 0x83, 0x01);
            Thread.Sleep(100);
            USB.SendCMD(Data.OnlyId, 0x83, 0x00);
        }

        bool ExecDec = false;
        int count = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Data.AdFrmIsAlive)
            {
                for (int i = 0; i < 8; i++)
                {
                    Data.dt_AD01.Rows[i]["测量值"] = Data.daRe_AD01[i];
                    Data.dt_AD02.Rows[i]["测量值"] = Data.daRe_AD02[i];

                    MyPane.CurveList[i].AddPoint(count, Data.daRe_AD01[i]);
                    MyPane.CurveList[i+8].AddPoint(count, Data.daRe_AD02[i]);
                }
                count++;
            }

            zedGraphControl1.AxisChange();
            zedGraphControl1.Invalidate();


        }

        private void 查看AD数据ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Data.AdFrmIsAlive)
            {
                myADForm.Activate();
            }
            else
            {
                myADForm = new ADForm();
            }
            myADForm.Show();
        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }


        private void btn_Output_Click(object sender, EventArgs e)
        {
            byte[] RSend = new byte[8 + 32];
            RSend[0] = 0x1D;
            RSend[1] = 0x0D;
            RSend[2] = 0x00;
            RSend[3] = 0x20;//0x0020 = 32

            // for (int i = 0; i < 32; i++) RSend[164 + i] = 0x0;

            RSend[36] = 0xC0;
            RSend[37] = 0xDE;
            RSend[38] = 0xC0;
            RSend[39] = 0xDE;



            for (int i = 0; i < 8; i++)
            {
                double value = (double)dtR1.Rows[i]["电阻"];

                if (value == 20) value = 19.99;

                int RValue = (int)(1024 * value);
                int SendR = 0;
                SendR = 0x400 + ((RValue / 20) & 0x3ff);// + Data.value_R2[row + 24 * V];


                RSend[4+0 + 4 * i] = 0x00;
                RSend[4+1 + 4 * i] = 0x00;
                //RSend[4+2 + 4 * i] = (byte)((SendR & 0xff00) >> 8);
                //RSend[4+3 + 4 * i] = (byte)(SendR & 0xff);

                RSend[4 + 2 + 4 * i] = (byte)(SendR & 0xff);
                RSend[4 + 3 + 4 * i] =  (byte)((SendR & 0xff00) >> 8);

            }


            Register.Byte84H = (byte)(Register.Byte84H | 0x40);
            USB.SendCMD(Data.OnlyId, 0x84, Register.Byte84H);
            Register.Byte84H = (byte)(Register.Byte84H & 0x3f);
            USB.SendCMD(Data.OnlyId, 0x84, 0x00);

            USB.SendData(Data.OnlyId, RSend);

            Thread.Sleep(500);//延时500ms再发送一次，确保设置到位

            Register.Byte84H = (byte)(Register.Byte84H | 0x40);
            USB.SendCMD(Data.OnlyId, 0x84, Register.Byte84H);
            Register.Byte84H = (byte)(Register.Byte84H & 0x3f);
            USB.SendCMD(Data.OnlyId, 0x84, 0x00);

            USB.SendData(Data.OnlyId, RSend);
            string temp1 = "";
            for (int i = 0; i < 40; i++) temp1 += RSend[i].ToString("x2");
            Trace.WriteLine("Board1:" + temp1);


        }

        private void dataGridView_R1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            string SenderName = dgv.Name;
            int LastNameValue = 0;



            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 2)
                {
                    double t = 0;
                    string temp = dataGridView_R1.Rows[e.RowIndex].Cells[2].FormattedValue.ToString();
                    bool ret = double.TryParse(temp, out t);
                    if (t < 0 || t > 20 || ret == false)
                    {
                        dataGridView_R1.Rows[e.RowIndex].Cells[2].Value = "20";
                    }

                }
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
    
                foreach (DataRow dr in dtR1.Rows)
                {
                    dr["电阻"] = numericUpDown4.Value;
                }
            
        }

        private void groupBox6_Enter(object sender, EventArgs e)
        {

        }

        private void button3_Click_2(object sender, EventArgs e)
        {
            this.textBox_Serial01.Clear();
            this.textBox_Serial02.Clear();
            this.textBox_Serial03.Clear();
            this.textBox_Serial04.Clear();
            this.textBox_Serial05.Clear();
            this.textBox_Serial06.Clear();
            this.textBox_Serial07.Clear();
            this.textBox_Serial08.Clear();
            this.textBox_Serial09.Clear();
            this.textBox_Serial10.Clear();
            this.textBox_Serial11.Clear();
            this.textBox_Serial12.Clear();
            this.textBox_Serial13.Clear();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            this.textBox_Serial01.Clear();
            this.textBox_Serial02.Clear();
            this.textBox_Serial03.Clear();
            this.textBox_Serial04.Clear();
            this.textBox_Serial05.Clear();
            this.textBox_Serial06.Clear();
            this.textBox_Serial07.Clear();
            this.textBox_Serial08.Clear();
            this.textBox_Serial09.Clear();
            this.textBox_Serial10.Clear();
            this.textBox_Serial11.Clear();
            this.textBox_Serial12.Clear();
            this.textBox_Serial13.Clear();
        }
    }


}
