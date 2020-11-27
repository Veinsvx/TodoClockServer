using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;


namespace TodoClockServer
{

    class Program
    {
        public static Socket socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static string serverIp = "0.0.0.0";//设定IP
        public static int serverPort = 2333;//设定端口
        public static byte[] result = new byte[10240];//定义一个字节数组,相当于缓存

        static bool ifFlag = true;
        static bool whileFlag = true;
        static bool up_ifFlag = true;

        static void Main(string[] args)
        {
            //创建socket服务
            IPAddress ip = IPAddress.Parse(serverIp);//服务器IP地址
            IPEndPoint point = new IPEndPoint(ip, serverPort);//获取端口
            socketServer.Bind(point);//绑定IP地址及端口
            socketServer.Listen(5);//开启监听并设定最多5个排队连接请求 
            Console.WriteLine("启动监听{0}成功", socketServer.LocalEndPoint.ToString());//输出启动成功提示
            Thread thread = new Thread(ListenSocket);//创建一个监听进程
            thread.Start();//运行
        }

        /// <summary>
        /// 监听客户端连接，每当有新用户连接，就分配一个线程专门处理
        /// </summary>
        public static void ListenSocket()
        {
            while (true)
            {
                Socket clientSocket = socketServer.Accept();//接收连接并返回一个新的Socket             
                Thread recevieThread = new Thread(OnNewConnection);//创建一个接受信息的进程
                recevieThread.Start(clientSocket);//运行新的Socket接受信息
            }
        }


        /// <summary>
        ///  当新的客户端连入时会调用这个方法
        /// </summary>
        /// <param name="clientSocket"></param>
        public static void OnNewConnection(object clientSocket)
        {
            Socket SocketClient = (Socket)clientSocket;
            Console.WriteLine("新设备{0}连接成功", SocketClient.RemoteEndPoint.ToString());
            while (true)
            {
                //接受消息头（消息校验码4字节 + 消息长度4字节 + 身份ID8字节 + 主命令4字节 + 子命令4字节 + 加密方式4字节 = 28字节）
                int HeadLength = 28;
                //存储消息头的所有字节数
                byte[] recvBytesHead = new byte[HeadLength];
                //如果当前需要接收的字节数大于0，则循环接收
                while (HeadLength > 0)
                {
                    byte[] recvBytes1 = new byte[28];
                    //将本次传输已经接收到的字节数置0
                    int iBytesHead = 0;
                    //如果当前需要接收的字节数大于缓存区大小，则按缓存区大小进行接收，相反则按剩余需要接收的字节数进行接收
                    if (HeadLength >= recvBytes1.Length)
                    {
                        iBytesHead = SocketClient.Receive(recvBytes1, recvBytes1.Length, 0);
                    }
                    else
                    {
                        iBytesHead = SocketClient.Receive(recvBytes1, HeadLength, 0);
                    }
                    //将接收到的字节数保存
                    recvBytes1.CopyTo(recvBytesHead, recvBytesHead.Length - HeadLength);
                    //减去已经接收到的字节数
                    HeadLength -= iBytesHead;
                }
                //接收消息体（消息体的长度存储在消息头的4至8索引位置的字节里）
                byte[] bytes = new byte[4];
                Array.Copy(recvBytesHead, 4, bytes, 0, 4);
                int BodyLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0));
                //存储消息体的所有字节数
                byte[] recvBytesBody = new byte[BodyLength];
                //如果当前需要接收的字节数大于0，则循环接收
                while (BodyLength > 0)
                {
                    byte[] recvBytes2 = new byte[BodyLength < 1024 ? BodyLength : 1024];
                    //将本次传输已经接收到的字节数置0
                    int iBytesBody = 0;
                    //如果当前需要接收的字节数大于缓存区大小，则按缓存区大小进行接收，相反则按剩余需要接收的字节数进行接收
                    if (BodyLength >= recvBytes2.Length)
                    {
                        iBytesBody = SocketClient.Receive(recvBytes2, recvBytes2.Length, 0);
                    }
                    else
                    {
                        iBytesBody = SocketClient.Receive(recvBytes2, BodyLength, 0);
                    }
                    //将接收到的字节数保存
                    recvBytes2.CopyTo(recvBytesBody, recvBytesBody.Length - BodyLength);
                    //减去已经接收到的字节数
                    BodyLength -= iBytesBody;
                }
                //一个消息包接收完毕，解析消息包
                Console.Write("从设备{0}", SocketClient.RemoteEndPoint);
                string[] strTemp = UnpackData(recvBytesHead, recvBytesBody);
                if (strTemp[0] == "下载")
                {
                    Console.WriteLine("设备{0}点击了下载", SocketClient.RemoteEndPoint);
                    string[] JsonStr = new string[2];
                    JsonStr[0] = CoreManger.ReadJsonFun($"{System.Environment.CurrentDirectory}" + "\\TodoList.json");
                    JsonStr[1] = CoreManger.ReadJsonFun($"{System.Environment.CurrentDirectory}" + "\\ClockTime.json");
                    byte[] sendBytes = BuildDataPackage(1, 233, 3, 4, 5, JsonStr);
                    Console.WriteLine("要发送给设备{0}的数据包的总长度为为{1}", SocketClient.RemoteEndPoint, sendBytes.Length);
                    SocketClient.Send(sendBytes, sendBytes.Length, 0);
                    Console.WriteLine("发送给设备{0}的Todo数据为{1}，Clock数据为{2}", SocketClient.RemoteEndPoint, JsonStr[0], JsonStr[1]);
                }
                else
                {
                    if(strTemp.Length>2)
                    {
                        File.WriteAllText($"{System.Environment.CurrentDirectory}" + "\\TodoList.json", strTemp[0]);
                        File.WriteAllText($"{System.Environment.CurrentDirectory}" + "\\ClockTime.json", strTemp[1]);
                        Console.WriteLine("设备{0}发送过来的Json文件写入完成", SocketClient.RemoteEndPoint);
                    }
                    else
                    {
                        Console.WriteLine("设备{0}发送过来的数据可能不完整",SocketClient.RemoteEndPoint);
                    }                   
                }
                break;
            }

            Console.WriteLine("与设备{0}断开连接，主连接线程结束", SocketClient.RemoteEndPoint);
            SocketClient.Close();
            whileFlag = true;
            ifFlag = true;
        }


        /// <summary>
        /// 解析消息包
        /// </summary>
        /// <param name="Head">消息头</param>
        /// <param name="Body">消息体</param>
        public static string[] UnpackData(byte[] Head, byte[] Body)
        {
            byte[] bytes = new byte[4];
            Array.Copy(Head, 0, bytes, 0, 4);
            //Console.WriteLine("接收到数据包中的校验码为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0)));

            bytes = new byte[8];
            Array.Copy(Head, 8, bytes, 0, 8);
            //Console.WriteLine("接收到数据包中的身份ID为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt64(bytes, 0)));

            bytes = new byte[4];
            Array.Copy(Head, 16, bytes, 0, 4);
            //Console.WriteLine("接收到数据包中的数据主命令为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0)));

            bytes = new byte[4];
            Array.Copy(Head, 20, bytes, 0, 4);
            //Console.WriteLine("接收到数据包中的数据子命令为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0)));

            bytes = new byte[4];
            Array.Copy(Head, 24, bytes, 0, 4);
            //Console.WriteLine("接收到数据包中的数据加密方式为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0)));

            bytes = new byte[Body.Length];
            string[] strmsg = new string[Body.Length];
            for (int i = 0,j=0; i < Body.Length;)
            {
                byte[] _byte = new byte[4];
                Array.Copy(Body, i, _byte, 0, 4);
                i += 4;
                int num = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_byte, 0));

                _byte = new byte[num];
                Array.Copy(Body, i, _byte, 0, num);
                i += num;
                Console.WriteLine("接收到数据包中的数据有：" + Encoding.UTF8.GetString(_byte, 0, _byte.Length));
                strmsg[j] = Encoding.UTF8.GetString(_byte, 0, _byte.Length);
                j += 1;
            }
            return strmsg;
        }

        /// <summary>
        /// 构建消息数据包
        /// </summary>
        /// <param name="Crccode">消息校验码，判断消息开始</param>
        /// <param name="sessionid">用户登录成功之后获得的身份ID</param>
        /// <param name="command">主命令</param>
        /// <param name="subcommand">子命令</param>
        /// <param name="encrypt">加密方式</param>
        /// <param name="MessageBody">消息内容（string数组）</param>
        /// <returns>返回构建完整的数据包</returns>
        public static byte[] BuildDataPackage(int Crccode, long sessionid, int command, int subcommand, int encrypt, string[] MessageBody)
        {
            //消息校验码默认值为0x99FF
            Crccode = 65433;
            //消息头各个分类数据转换为字节数组（非字符型数据需先转换为网络序  HostToNetworkOrder:主机序转网络序）
            byte[] CrccodeByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Crccode));
            byte[] sessionidByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(sessionid));
            byte[] commandByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(command));
            byte[] subcommandByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(subcommand));
            byte[] encryptByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(encrypt));
            //计算消息体的长度
            int MessageBodyLength = 0;
            for (int i = 0; i < MessageBody.Length; i++)
            {
                if (MessageBody[i] == "")
                    break;
                MessageBodyLength += Encoding.UTF8.GetBytes(MessageBody[i]).Length;
            }
            //定义消息体的字节数组（消息体长度MessageBodyLength + 每个消息前面有一个int变量记录该消息字节长度）
            byte[] MessageBodyByte = new byte[MessageBodyLength + MessageBody.Length * 4];
            //记录已经存入消息体数组的字节数，用于下一个消息存入时检索位置
            int CopyIndex = 0;
            for (int i = 0; i < MessageBody.Length; i++)
            {
                //单个消息
                byte[] bytes = Encoding.UTF8.GetBytes(MessageBody[i]);
                //先存入单个消息的长度
                BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length)).CopyTo(MessageBodyByte, CopyIndex);
                CopyIndex += 4;
                bytes.CopyTo(MessageBodyByte, CopyIndex);
                CopyIndex += bytes.Length;
            }
            //定义总数据包（消息校验码4字节 + 消息长度4字节 + 身份ID8字节 + 主命令4字节 + 子命令4字节 + 加密方式4字节 + 消息体）
            byte[] totalByte = new byte[28 + MessageBodyByte.Length];
            //组合数据包头部（消息校验码4字节 + 消息长度4字节 + 身份ID8字节 + 主命令4字节 + 子命令4字节 + 加密方式4字节）
            CrccodeByte.CopyTo(totalByte, 0);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(MessageBodyByte.Length)).CopyTo(totalByte, 4);
            sessionidByte.CopyTo(totalByte, 8);
            commandByte.CopyTo(totalByte, 16);
            subcommandByte.CopyTo(totalByte, 20);
            encryptByte.CopyTo(totalByte, 24);
            //组合数据包体
            MessageBodyByte.CopyTo(totalByte, 28);
            //Console.WriteLine("发送数据包的总长度为：" + totalByte.Length);
            return totalByte;
        }


    }
}
