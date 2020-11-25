using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace TodoClockServer
{

    class Program
    {
        public static Socket socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static string serverIp = "0.0.0.0";//设定IP
        public static int serverPort = 2333;//设定端口
        public static byte[] result = new byte[1024];//定义一个字节数组,相当于缓存

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
            while (whileFlag)
            {
                if (ifFlag)
                {
                    int num = SocketClient.Receive(result);//把接受到的数据存到bytes数组中并赋值给num
                    string msg = Encoding.UTF8.GetString(result, 0, num);
                    Console.WriteLine("接收设备{0} 的消息：{1}", SocketClient.RemoteEndPoint.ToString(), msg);//GetString(result, 0, num)把从0到num的字节变成String
                    if (msg == "UP")
                    {
                        Thread recevieThread = new Thread(UpConTroll);//创建一个接受信息的进程
                        recevieThread.Start(clientSocket);//运行新的Socket接受信息
                        ifFlag = false;
                    }
                    else if(msg == "Down")
                    {

                    }

                }
            }
            Console.WriteLine("与设备{0}断开连接，主连接线程结束", SocketClient.RemoteEndPoint);
            SocketClient.Close();
            whileFlag = true;
            ifFlag = true;      
        }

        public static void UpConTroll(object obj)
        {
            Socket SocketClient = (Socket)obj;
            SocketClient.Send(Encoding.UTF8.GetBytes("con"));//给客户端发送信息
            while (true)
            {
                if (up_ifFlag)
                {
                    int num = SocketClient.Receive(result);//把接受到的数据存到bytes数组中并赋值给num
                    string msg = Encoding.UTF8.GetString(result, 0, num);
                    Console.WriteLine("在上传接收线程中，设备{0}发送的消息是：{1}", SocketClient.RemoteEndPoint.ToString(), msg);
                    if (msg == "todo")
                    {
                        Thread recevieThread = new Thread(TodoStart);//创建一个接收todojson的进程
                        recevieThread.Start(obj);//运行新的Socket接受信息
                        up_ifFlag = false;
                    }
                    else if (msg == "clock")
                    {
                        Thread recevieThread = new Thread(ClockStart);//创建一个接收todojson的进程
                        recevieThread.Start(obj);//运行新的Socket接受信息
                        up_ifFlag = false;
                    }
                    else if (msg == "exit")
                    {
                        whileFlag = false;//连接传输完就直接退出
                        break;
                    }
                }
            }
            Console.WriteLine("设备{0}开启的线程UpConTroll已退出",SocketClient.RemoteEndPoint);
        }

        private static void ClockStart(object obj)
        {
            Socket SocketClient = (Socket)obj;
            SocketClient.Send(Encoding.UTF8.GetBytes("todoStart"));//给客户端发送信息
            while (true)
            {
                int num = SocketClient.Receive(result);//把接受到的数据存到bytes数组中并赋值给num
                string msg = Encoding.UTF8.GetString(result, 0, num);
                //用包头方式接收完数据并退出循环结束线程
                break;
            }
            up_ifFlag = true;
            SocketClient.Send(Encoding.UTF8.GetBytes("todoCP"));
            Console.WriteLine("接收设备{0}的todoJson文件的线程结束了", SocketClient.RemoteEndPoint);
        }

        private static void TodoStart(object obj)
        {
            Socket SocketClient = (Socket)obj;
            SocketClient.Send(Encoding.UTF8.GetBytes("clockStart"));//给客户端发送信息
            while (true)
            {
                int num = SocketClient.Receive(result);//把接受到的数据存到bytes数组中并赋值给num
                string msg = Encoding.UTF8.GetString(result, 0, num);
                //用包头方式接收完数据并退出循环结束线程
                break;
            }
            up_ifFlag = true;
            SocketClient.Send(Encoding.UTF8.GetBytes("clockCP"));
            Console.WriteLine("接收设备{0}的ClockJson文件的线程结束了", SocketClient.RemoteEndPoint);
        }
    }

}
