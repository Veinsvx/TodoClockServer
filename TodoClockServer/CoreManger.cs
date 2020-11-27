using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoClockServer
{
    public struct SocketHead
    {
        //起始位，表示字节的开始
        public byte StartFlag;
        //校验位，检验数据是否正确
        public byte CheckNum;
        //协议位，表示需要执行什么功能
        public byte Cmd;
        //消息体数据长度
        public int Length;
    }
    class CoreManger
    {
        public static void IntoBytes(byte[] data, int offset, int value)
        {
            //将int类型以数组方式添加到目标数组
            data[offset++] = (byte)(value);
            data[offset++] = (byte)(value >> 8);
            data[offset++] = (byte)(value >> 16);
            data[offset] = (byte)(value >> 24);
        }

        public static int ToInt32(byte[] data, int offset)
        {
            //bytes数据长度转成int类型
            return (int)(data[offset++] | data[offset++] << 8 | data[offset++] << 16 | data[offset] << 24);
        }

        public static byte[] BuildData(byte cmd, byte[] data)
        {//构建数据包头
            byte[] buffer = new byte[7 + data.Length];

            byte startFlag = 0xF;
            //起始位
            buffer[0] = startFlag;
            //指令位
            buffer[1] = cmd;
            //校验位
            buffer[2] = (byte)(cmd + startFlag);

            IntoBytes(buffer, 3, data.Length);

            Array.Copy(data, 0, buffer, 7, data.Length);

            return buffer;
        }

        public static bool ParseHead(byte[] data, out SocketHead socketHead)
        {//解析数据包头
            if (data.Length >= 7)
            {

                socketHead = new SocketHead
                {
                    StartFlag = data[0],
                    Cmd = data[1],
                    CheckNum = data[2]
                };
                //验证数据是否正确
                if (socketHead.CheckNum == socketHead.StartFlag + socketHead.Cmd)
                {
                    Console.WriteLine("jinru");
                    socketHead.Length = ToInt32(data, 3);
                    return true;
                }
                return false;
            }
            socketHead = new SocketHead();
            return false;
        }
        /// <summary>
        /// 读取本地todoJson文件，将其转换为string类型
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ReadJsonFun(string strPath)
        {
            string dataAsJson = "";
            dataAsJson = File.ReadAllText(strPath, Encoding.UTF8);
            if (dataAsJson != null)
            {
                return dataAsJson;
            }
            return dataAsJson;
        }

        public static string Decodeing(string s)
        {
            Regex reUnicode = new Regex(@"\\u([0-9a-fA-F]{4})", RegexOptions.Compiled);
            return reUnicode.Replace(s, m =>
            {
                short c;
                if (short.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out c))
                {
                    return "" + (char)c;
                }
                return m.Value;
            });

        }
    }
}
