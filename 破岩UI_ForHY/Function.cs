using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace 破岩UI_ForHY
{
    class Function
    {

        /// <summary>
        /// 修改AppSettings中配置
        /// </summary>
        /// <param name="key">key值</param>
        /// <param name="value">相应值</param>
        public static bool SetConfigValue(string key, string value)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings[key] != null)
                    config.AppSettings.Settings[key].Value = value;
                else
                    config.AppSettings.Settings.Add(key, value);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static ushort CRChware(ushort data, ushort genpoly, ushort crc)
        {
            int i = 8;
            data <<= 8;
            for (i = 8; i > 0; i--)
            {
                if (((data ^ crc) & 0x8000) != 0)
                {
                    crc = (ushort)((crc << 1) ^ genpoly);
                }
                else
                {
                    crc <<= 1;
                }
                data <<= 1;
            }
            return crc;
        }

        public static ushort CRChware2(ushort data, ushort genpoly, ushort crc)
        {
            int wTemp = 0;
            for (int i = 0; i < 8; i++)
            {
                wTemp = ((data << i) & 0x80) ^ ((crc & 0x8000) >> 8);
                crc <<= 1;
                if (wTemp != 0) crc ^= genpoly;
            }
            return crc;
        }

        /// <summary>
        /// 异或和
        /// </summary>
        /// <returns></returns>
        public static ushort Multiware()
        {
            //
            return 1;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SystemTime
        {
            public ushort vYear;
            public ushort vMonth;
            public ushort vDayofWeek;
            public ushort vDay;
            public ushort vHour;
            public ushort vMinute;
            public ushort vSecond;
            public ushort vMilliseconds;
        }
        //-----------------------------------------------
        [DllImport("Kernel32.dll")]
        public extern static void GetLocalTime(ref SystemTime lpSystemTime);
        [DllImport("Kernel32.dll")]
        public extern static void SetLocalTime(ref SystemTime lpSystemTime);
        //===============================================

        //---------获取系统时间---------
        public static byte[] Get_Time()
        {
            byte[] time_span = new byte[6];

            DateTime nowtime = DateTime.Now;
            DateTime start_time = new DateTime(2000, 1, 1, 0, 0, 0);
            TimeSpan ts = nowtime - start_time;

            UInt16 days = (UInt16)ts.Days;
            UInt32 ms = (UInt32)(ts.TotalMilliseconds - ts.Days * 24 * 60 * 60 * 1000) * 10;//??

            time_span[0] = (byte)(days & 0x00FF);
            time_span[1] = (byte)((days & 0xFF00) >> 8);
            time_span[2] = (byte)(ms & 0x000000FF);
            time_span[3] = (byte)((ms & 0x0000FF00) >> 8);
            time_span[4] = (byte)((ms & 0x00FF0000) >> 16);
            time_span[5] = (byte)((ms & 0xFF000000) >> 24);

            return time_span;
        }


        public static string DecodeTime(byte[] time_span)
        {
            string timestr = "";

            DateTime start_time = new DateTime(2000, 1, 1, 0, 0, 0);

            int SpentS = time_span[0] << 24 + time_span[1] << 16 + time_span[2] << 8 + time_span[3];
            DateTime nowtime = start_time.AddSeconds(SpentS);

            int ms_orginal = time_span[4] << 8 + time_span[3];
            string ms = (ms_orginal / 10).ToString();
            timestr = nowtime.ToString("yyyy-MM-dd HH:mm:ss");
            timestr += ":" + ms;
            return timestr;
        }


        //----------设置系统时间----------
        public static void Set_Time(byte[] data)
        {
            try
            {
                byte[] gettime = new byte[23];
                for (int datai = 0; datai < data.Length; datai++)
                {
                    gettime[datai] = byte.Parse(data[datai].ToString());
                }
                string dt_str = Encoding.ASCII.GetString(gettime);

                string str = dt_str.Substring(0, 4);
                SystemTime systime = new SystemTime();
                systime.vYear = ushort.Parse(str);

                str = dt_str.Substring(5, 2);
                systime.vMonth = ushort.Parse(str);

                str = dt_str.Substring(8, 2);
                systime.vDay = ushort.Parse(str);

                str = dt_str.Substring(11, 2);
                systime.vHour = ushort.Parse(str);

                str = dt_str.Substring(14, 2);
                systime.vMinute = ushort.Parse(str);

                str = dt_str.Substring(17, 2);
                systime.vSecond = ushort.Parse(str);

                str = dt_str.Substring(20, 3);
                systime.vMilliseconds = ushort.Parse(str);
                //--------更改系统时间--------
                SetLocalTime(ref systime);
            }
            catch
            {
                MyLog.Error("设置系统时间失败！");
            }
        }

        /// <summary>
        /// 获取配置文件中的内容
        /// </summary>
        /// <param name="Path">xml路径</param>
        /// <param name="type">节点：'add'</param>
        /// <param name="key">key</param>
        /// <param name="name">要获取的属性名称</param>
        /// <returns></returns>
        public static string GetConfigStr(string Path, string type, string key, string name)
        {
            XDocument xDoc = XDocument.Load(Path);
            XmlReader reader = xDoc.CreateReader();
            string value = "Error";
            var query = from p in xDoc.Root.Elements(type)
                        where p.Attribute("key").Value == key
                        select p.Attribute(name).Value;

            foreach (string s in query)
            {
                value = s;
            }
            return value;
        }


        public static void SaveConfigStr(string Path, string type, string key, string name, string value)
        {
            XDocument xDoc = XDocument.Load(Path);
            XmlReader reader = xDoc.CreateReader();

            bool Matched = false;//是否已在XML中

            foreach (var p in xDoc.Root.Elements(type))
            {
                if (p.Attribute("key").Value == key)
                {
                    p.Attribute(name).Value = value;
                    Matched = true;
                }
            }
            if (Matched == false)
            {
                XElement element = new XElement(type, new XAttribute("key", key), new XAttribute(name, value));
                xDoc.Root.Add(element);
            }
            xDoc.Save(Path);
        }


        /// <summary>
        /// 获取所有的key的名称
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<string> GetConfigNormal(string Path, string type)
        {
            XDocument xDoc = XDocument.Load(Path);
            XmlReader reader = xDoc.CreateReader();

            var query = from p in xDoc.Root.Elements(type)
                        select p.Attribute("key").Value;

            List<string> list = new List<string>();
            foreach (string s in query)
            {
                list.Add(s);
            }
            return list;
        }


        /// <summary>
        /// 十六进制String转化为BYTE数组
        /// </summary>
        /// <param name="hexString">参数：输入的十六进制String</param>
        /// <returns>BYTE数组</returns>
        public static byte[] StrToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "").Replace("\r", "").Replace("\n", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";

            byte[] returnBytes = new byte[hexString.Length / 2];

            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;

        }

    }
}
