using System;

using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Linq;
using System.Timers;
using System.Net;
using System.Net.Sockets;



namespace SerialPortW
{
    public partial class SerialPortW : Form
    {
        //定义常量
        private const byte SOH = 0x01;
        private const byte STX = 0x02;
        private const byte EOT = 0x04;
        private const byte ACK = 0x06;
        private const byte NAK = 0x15;
        private const byte CAN = 0x18;
        private const byte CTRLZ = 0x1A;
        private const byte C = 0x43;
        private SerialPort[] serialPort;//串口序列 
        private String filePath;//传输文件路径
        private int ComNum;//串口总数
        private String[] serialPortArray;//串口名字序列
        private ProgressBar[] progressbar;//每个串口烧写进度条
        private int[] achievedCount;//传输完成数
        private int requestedCount= System.Int32.MaxValue;//传输总数
        private Button[] Xbutton;//为每个串口生成的下载按键
        private Label[] label;//标签序列
        private BackgroundWorker[] BGW;//为串口烧写生成线程
        private int[] XTF_Outcome;//记录每个窗口的传输结果
        private string[] buffer;//最新从缓存接收的字符串
        static private String ErrorLogPath = @"C:\Users\pcdalao\Desktop\";//错误日志路径
        FileStream[] logInfo;//错误日志文件流序列
        private int missionNum;//正在进行的传输任务

        private int[,] COM_Position;//记录不同COM端口烧写按钮和标签的位置信息


        //crc table
        public static readonly ushort[] crc16tab = new ushort[256]{
            0x0000, 0xC0C1, 0xC181, 0x0140, 0xC301, 0x03C0, 0x0280, 0xC241,
            0xC601, 0x06C0, 0x0780, 0xC741, 0x0500, 0xC5C1, 0xC481, 0x0440,
            0xCC01, 0x0CC0, 0x0D80, 0xCD41, 0x0F00, 0xCFC1, 0xCE81, 0x0E40,
            0x0A00, 0xCAC1, 0xCB81, 0x0B40, 0xC901, 0x09C0, 0x0880, 0xC841,
            0xD801, 0x18C0, 0x1980, 0xD941, 0x1B00, 0xDBC1, 0xDA81, 0x1A40,
            0x1E00, 0xDEC1, 0xDF81, 0x1F40, 0xDD01, 0x1DC0, 0x1C80, 0xDC41,
            0x1400, 0xD4C1, 0xD581, 0x1540, 0xD701, 0x17C0, 0x1680, 0xD641,
            0xD201, 0x12C0, 0x1380, 0xD341, 0x1100, 0xD1C1, 0xD081, 0x1040,
            0xF001, 0x30C0, 0x3180, 0xF141, 0x3300, 0xF3C1, 0xF281, 0x3240,
            0x3600, 0xF6C1, 0xF781, 0x3740, 0xF501, 0x35C0, 0x3480, 0xF441,
            0x3C00, 0xFCC1, 0xFD81, 0x3D40, 0xFF01, 0x3FC0, 0x3E80, 0xFE41,
            0xFA01, 0x3AC0, 0x3B80, 0xFB41, 0x3900, 0xF9C1, 0xF881, 0x3840,
            0x2800, 0xE8C1, 0xE981, 0x2940, 0xEB01, 0x2BC0, 0x2A80, 0xEA41,
            0xEE01, 0x2EC0, 0x2F80, 0xEF41, 0x2D00, 0xEDC1, 0xEC81, 0x2C40,
            0xE401, 0x24C0, 0x2580, 0xE541, 0x2700, 0xE7C1, 0xE681, 0x2640,
            0x2200, 0xE2C1, 0xE381, 0x2340, 0xE101, 0x21C0, 0x2080, 0xE041,
            0xA001, 0x60C0, 0x6180, 0xA141, 0x6300, 0xA3C1, 0xA281, 0x6240,
            0x6600, 0xA6C1, 0xA781, 0x6740, 0xA501, 0x65C0, 0x6480, 0xA441,
            0x6C00, 0xACC1, 0xAD81, 0x6D40, 0xAF01, 0x6FC0, 0x6E80, 0xAE41,
            0xAA01, 0x6AC0, 0x6B80, 0xAB41, 0x6900, 0xA9C1, 0xA881, 0x6840,
            0x7800, 0xB8C1, 0xB981, 0x7940, 0xBB01, 0x7BC0, 0x7A80, 0xBA41,
            0xBE01, 0x7EC0, 0x7F80, 0xBF41, 0x7D00, 0xBDC1, 0xBC81, 0x7C40,
            0xB401, 0x74C0, 0x7580, 0xB541, 0x7700, 0xB7C1, 0xB681, 0x7640,
            0x7200, 0xB2C1, 0xB381, 0x7340, 0xB101, 0x71C0, 0x7080, 0xB041,
            0x5000, 0x90C1, 0x9181, 0x5140, 0x9301, 0x53C0, 0x5280, 0x9241,
            0x9601, 0x56C0, 0x5780, 0x9741, 0x5500, 0x95C1, 0x9481, 0x5440,
            0x9C01, 0x5CC0, 0x5D80, 0x9D41, 0x5F00, 0x9FC1, 0x9E81, 0x5E40,
            0x5A00, 0x9AC1, 0x9B81, 0x5B40, 0x9901, 0x59C0, 0x5880, 0x9841,
            0x8801, 0x48C0, 0x4980, 0x8941, 0x4B00, 0x8BC1, 0x8A81, 0x4A40,
            0x4E00, 0x8EC1, 0x8F81, 0x4F40, 0x8D01, 0x4DC0, 0x4C80, 0x8C41,
            0x4400, 0x84C1, 0x8581, 0x4540, 0x8701, 0x47C0, 0x4680, 0x8641,
            0x8201, 0x42C0, 0x4380, 0x8341, 0x4100, 0x81C1, 0x8081, 0x4040
            };



        //定义委托SetTextCallback, 返回值是void，参数类型String
        delegate void SetTextCallback(String text);
        //跨线程调用控件
        private delegate void InvokeCallback(string msg);

        // 构造函数
        public SerialPortW()
        {
            InitializeComponent();
        }

        // Form_Load 事件，程序打开Form的时候触发
        private void SerialPortW_Load(object sender, EventArgs e)
        {
            //这个类中我们不检查跨线程的调用是否合法
            Control.CheckForIllegalCrossThreadCalls = false;
            portInit();
        }

        private void portInit()
        {
            //获取串口
            getAvailablePort();
            //设置初始显示的值 

            // 发送端默认发送字节
            //TextBoxSend.Text = "abcdefghijklmnopqrstuvwxyz0123456789";

            //打开串口，生成面板
            AutoGeneratingControls();
            // 创建串口接收事件
            // 这里只需要创建一个事件就行,以前的操作每次点击open按键就创建一次，浪费资源
            // 操作中，通过，start()，stop()函数进行控制
            foreach(SerialPort sp in serialPort)
            {
                sp.DataReceived += new SerialDataReceivedEventHandler(dealReceive);//为每个串口添加接收中断
            }

        }

        private void getAvailablePort()
        {
            try
            {
                // 获取系统的串口
                serialPortArray = SerialPort.GetPortNames();
                // 电脑没有串口,抛出异常
                if ((ComNum = serialPortArray.Length) == 0)
                {
                    throw (new System.IO.IOException("This computer don't have serial port!"));
                }
                // 串口显示在comboBox中

                if (serialPortArray != null && serialPortArray.Length != 0)
                {
                    //对串口进行排序, 泛型的值是string，按照string类型进行排序
                    serialPortArray = serialPortArray.OrderBy(s => int.Parse(Regex.Match(s, @"\d+").Value)).ToArray();//按照字符串中数字排序
                    //Array.Sort<String>(serialPortArray);按字符顺序排序
                    ComNum = serialPortArray.Length;//获取串口数
                    achievedCount = new int[ComNum];//为完成数组分配空间，每一个代表不同串口完成的输出字节数
                }
            }
            catch (System.IO.IOException e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void dealReceive(object sender, SerialDataReceivedEventArgs e)//接收窗口用串口1来测试
        {
            SerialPort tempPort = (SerialPort)sender;//收到数据的串口
            if (tempPort.IsOpen == true)//串口正确打开
            {
                // 读取缓存中的数据,不管有没有'\n'
                String data = tempPort.ReadExisting();
                int Pointer = Array.IndexOf(serialPortArray, tempPort.PortName);//找到串口在串口序列下标
                if(Pointer==-1)//未找到
                {
                    MessageBoxButtons messButton = MessageBoxButtons.OK;
                    DialogResult dr = MessageBox.Show("找不到串口！请重新打开尝试！", "串口打开错误", messButton);
                    return;
                }

                if (buffer[Pointer].Length < 500) buffer[Pointer] += data;//如果以后要开多线程，就要设一个数组来存每一个串口收到的数据
                else buffer[Pointer] = buffer[Pointer].Substring(buffer[Pointer].Length - 100);//缓存满了，截取后面200个字节
                if (data != String.Empty)//考虑多线程的情况
                {
                    // 线程更新UI
                    this.BeginInvoke(new SetTextCallback(SetText), new object[] { data });
                }
            }
        }

        private void SetText(String text)
        {
            // 设置焦点到richTextBoxReceive
            //richTextBoxReceive.Focus();
            // 需要手动设置richTextBox的滚动条，否则不会自动更新,只显示最开始的内容
            // 设置光标的位置到文本尾

            richTextBoxReceive.Select(richTextBoxReceive.TextLength, 0);
            // 滚动到控件光标处   
            richTextBoxReceive.ScrollToCaret();
            // 在末尾添加新内容
            richTextBoxReceive.AppendText(text);
        }

        private void setPort(int i)//设置串口序列中的第i个串口
        {//添加错误日志
            try
            {
                serialPort[i].PortName = serialPortArray[i];
                serialPort[i].BaudRate = 115200;
                serialPort[i].DataBits = 8;
                serialPort[i].StopBits = StopBits.One;
                serialPort[i].Parity = Parity.None;
            }
            catch (Exception ex)
            {
                AddText(logInfo[i], ex.ToString());//错误日志记录错误
                logInfo[i].Flush();//将缓存写入错误日志
            }

            try
            {
                serialPort[i].Open();
            }
            catch (UnauthorizedAccessException)
            {
                String err = "current port has been used by other application! \nor some other error may happened!";
                AddText(logInfo[i], err);//错误日志记录错误
                logInfo[i].Flush();//将缓存写入错误日志

            }
            catch (IOException)
            {
                // 如果程序打开时存在串口设备，就会显示在UI上
                // 但是如果将串口（例如usb转串口转换器）拔了，这个电脑就不存在串口，但UI还是显示这个设备存在
                // 打开时就会出现异常
                String err = "current port don't exist!";
                AddText(logInfo[i], err);//错误日志记录错误
                logInfo[i].Flush();//将缓存写入错误日志

            }
        }

        //清除接收框中的内容
        private void cleanText_Click(object sender, EventArgs e)
        {
            richTextBoxReceive.Clear();
        }

        // Todo: 模仿超级终端等软件，在接受框中输入能够与串口连接的设备进行交互
        // if you don't need it ,just comment or delete it!
        // 接受框按键事件处理
        private void richTextBoxReceive_KeyPress(object sender, KeyPressEventArgs e)
        {
            /*
            if (e.KeyChar == (char)13) // enter key  
            {
                //serialPort.Write("\r");
                //byte by = 0x0A; '\n'
                byte[] by = {0x0A };
                //byte by = 0x0D; '\r'
                //byte[] by = {0x0D };
                // enter
                serialPort.Write(by, 0, by.Length);
                //serialPort.DiscardInBuffer();
                //byte by = 0x0A;
                //serialPort.BytesToWrite(by);
                //serialPort.Write("\r\n");
                //serialport.wir.Write("\r\n");
                //rtbOutgoing.Text = "";
            }
            else if (e.KeyChar == (char)3)
            {
                MessageBox.Show("You pressed control + c");
                byte[] by = { 0x03 };
                serialPort.Write(by, 0, by.Length);
                //return true;
//                serialPort.Write('^C');
            }
            else if (e.KeyChar < 32 || e.KeyChar > 126)
            {
                e.Handled = true; // ignores anything else outside printable ASCII range
            }
            else
            {
                //ComPort.Write(e.KeyChar.ToString());
                serialPort.Write(e.KeyChar.ToString());
            }
            */
        }

        // Todo:
        // if you don't need it ,just comment or delete it!
        // 方向键按键事件处理,通过重载processCmdKey函数实现
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            /*
            //capture up arrow key
            if (keyData == Keys.Up)
            {
                MessageBox.Show("You pressed Up arrow key");
                return true;
            }
            //capture down arrow key
            if (keyData == Keys.Down)
            {
                MessageBox.Show("You pressed Down arrow key");
                return true;
            }
            //capture left arrow key
            if (keyData == Keys.Left)
            {
                MessageBox.Show("You pressed Left arrow key");
                return true;
            }
            //capture right arrow key
            if (keyData == Keys.Right)
            {
                MessageBox.Show("You pressed Right arrow key");
                return true;
            }
           if (keyData == (Keys.Control | Keys.C))
            {
                MessageBox.Show("You pressed control + c");
                return true;

            }
            */
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void button1_Click(object sender, EventArgs e)
        {

            openFileDialog1.InitialDirectory = @"C:\";//初始显示目录

            //下次打开对话框是否定位到上次打开的目录
            openFileDialog1.RestoreDirectory = true;

            //过滤文件类型
            openFileDialog1.Filter = "文本文件 (*.img)|*.img|所有文件 (*.*)|*.*";

            //FilterIndex 与 Filter 关联对应，用于设置默认显示的文件类型
            openFileDialog1.FilterIndex = 1;//默认是1，则默认显示的文件类型为*.img；如果设置为2，则默认显示的文件类型是*.*

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {

                //创建文件路径
                filePath = openFileDialog1.FileName;

                //文件不存在，报错
                if (!File.Exists(filePath))
                {
                    //这里应该是检查错误，报错
                    MessageBoxButtons messButton = MessageBoxButtons.OK;
                    DialogResult dr = MessageBox.Show("文件路径错误！", "文件打开错误", messButton);
                    return;
                }
                FileInfo file_Info = new FileInfo(filePath);
                requestedCount = (int)file_Info.Length;//选中文件总字节数
                textBox1.Text = "已选中:" + file_Info.Name + "文件";
            }
            else
            {
                //这里应该是检查错误，报错
                MessageBoxButtons messButton = MessageBoxButtons.OK;
                DialogResult dr = MessageBox.Show("文件选择失败！", "警告", messButton);
                return;
            }
        }

        //crc16计算
        public static ushort ComputeCrc(byte[] bytes, int length)
        {
            ushort crc = 0;
            for (int i = 0; i < length; ++i)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ crc16tab[index]);
            }
            return crc;
        }

        public void XmodemTransfer(object sender, EventArgs e)
        {
            //根据sender得到button的id，然后打开对应对口，进行传输
            Button b1 = (Button)sender;//将触发此事件的对象转换为该Button对象
            int index = Array.IndexOf(serialPortArray, b1.Name);//找到串口在串口序列下标

            Xbutton[index].Text = "正在烧写";//可能由于异步执行直接显示
            Xbutton[index].Enabled = false;//只读
            missionNum += 1;//任务开始，数量+1；

            BGW[index].RunWorkerAsync(index);//调用线程进行传输,后面同时开启多个线程，同时烧写

            serialPort[index].DataReceived += new SerialDataReceivedEventHandler(dealReceive);//烧写完毕再开启接收中断
            //烧写状态用全局变量来表示,1表示烧写已结束，无论成功或失败
            XTF_Outcome[index] = 1;//别忘了初始化和置零等操作
        }


        //使用Xmodem传输文件
        private bool XmodemUploadFile(object sender,int id)
        {
            BackgroundWorker worker = sender as BackgroundWorker;//调用此函数的线程

            bool flag = true;//flag表示传输结果

            if (!serialPort[id].IsOpen) setPort(id);//如果串口已关闭，重新打开

            if (serialPort[id] != null && serialPort[id].IsOpen)
            {
                //连续发送两个回车，进入Xmodem烧写模式
                serialPort[id].Write(new byte[] { 0x0d, 0x0d }, 0, 2);
            }
            else
            {
                flag = false;//传输失败
                AddText(logInfo[id], "串口未正确打开或找不到串口");//登记到错误日志
                logInfo[id].Flush();//将缓存写入错误日志
                return flag;
            }

            Thread.Sleep(100);

            serialPort[id].DataReceived -= new SerialDataReceivedEventHandler(dealReceive);//先关闭接收字符串中断,准备开始烧写


            /* sizes */
            const byte dataSize = 128;

            /* THE PACKET: 132 bytes */
            /* header: 3 bytes */
            // SOH
            int packetNumber = 0;
            int invertedPacketNumber = 255;
            /* data: 128 bytes */
            byte[] data = new byte[dataSize];
            /* footer: 1 byte */
            int checkSum = 0;

            int tmp = 0;//记录重复使用的中间变量

            /* get the file */
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            FileInfo file = new FileInfo(filePath);

            try
            {
                /* wait until receive NAK */
                while (serialPort[id].ReadByte() != NAK) ;

                /* send packets with a cycle until we send the last byte */
                int fileReadCount;
                do
                {
                    /* if this is the last packet fill the remaining bytes with 0 */
                    fileReadCount = fileStream.Read(data, 0, dataSize);
                    if (fileReadCount == 0) break;
                    if (fileReadCount != dataSize)//不足128字节用CTRLZ补齐
                    {
                        for (int i = fileReadCount; i < dataSize; i++)
                            data[i] = CTRLZ;
                        achievedCount[id] += fileReadCount;//完成输出字节等于总字节数
                    }
                    else
                        achievedCount[id] += dataSize;//完成输出字节数加一个dataSize

                    int CompletePercent = (int)(100 * achievedCount[id] / requestedCount);//计算百分比
                    worker.ReportProgress(CompletePercent, id);//传递参数

                    /* calculate packetNumber */
                    packetNumber++;
                    if (packetNumber > 255)
                        packetNumber -= 256;

                    /* calculate invertedPacketNumber */
                    invertedPacketNumber = 255 - packetNumber;

                    /* calculate checkSum */
                    checkSum = 0;
                    for (int i = 0; i < dataSize; i++)
                        checkSum += data[i];
                    checkSum = (byte)(checkSum & 0xff);

                    retrans://没收到重发
                    /* send the packet */
                    serialPort[id].Write(new byte[] { SOH }, 0, 1);
                    serialPort[id].Write(new byte[] { (byte)packetNumber }, 0, 1);
                    serialPort[id].Write(new byte[] { (byte)invertedPacketNumber }, 0, 1);
                    serialPort[id].Write(data, 0, dataSize);
                    serialPort[id].Write(new byte[] { (byte)checkSum }, 0, 1);

                    response:
                    /* wait for ACK */
                    if ((tmp = serialPort[id].ReadByte()) != ACK)
                    {
                        if (tmp == NAK)//收到NAK重发
                        {
                            goto retrans;
                        }
                        else if (tmp == CAN)
                        {
                            AddText(logInfo[id], "接收方强制结束，传输失败");//登记到错误日志
                            logInfo[id].Flush();//将缓存写入错误日志
                            flag = false;
                            goto end;
                        }
                        else
                        {
                            //重新接收直到收到上面三者其中一个为止
                            goto response;
                        }
                    }
                } while (dataSize == fileReadCount);

                /* send EOT (tell the downloader we are finished) */
                serialPort[id].Write(new byte[] { EOT }, 0, 1);
                /* get ACK (downloader acknowledge the EOT) */
                if (serialPort[id].ReadByte() != ACK)
                {
                    AddText(logInfo[id], "结束时未正确收到响应，传输失败");//登记到错误日志
                    logInfo[id].Flush();//将缓存写入错误日志
                    flag = false;
                    goto end;
                }
            }
            catch (TimeoutException)
            {
                AddText(logInfo[id], "传输超时");//登记到错误日志
                logInfo[id].Flush();//将缓存写入错误日志
                flag = false;
                goto end;            
            }



            Console.WriteLine("File transfer is succesful");
            MessageBoxButtons messButton1 = MessageBoxButtons.OK;
            DialogResult dr1 = MessageBox.Show(serialPortArray[id]+"烧写成功", "烧写结果", messButton1);
            end://结束后还原
            serialPort[id].Close();//关闭打开的这个串口
            achievedCount[id] = 0;//进度条置0
            fileStream.Close();//关闭文件流 
            return flag;

        }
        private static void AddText(FileStream fs, string value)//转化String->Bytes
        {
            value = '[' + DateTime.Now.ToString() + "]:" + value+"\r\n";//给错误日志加上时间信息和换行符
            byte[] info = new UTF8Encoding(true).GetBytes(value);
            fs.Write(info, 0, info.Length);
        }

        private void AutoGeneratingControls()
        {
            panel1.AutoScroll = true;  //为panel添加滚动条
            Xbutton = new Button[ComNum];//Xbutton为全局变量
            label = new Label[ComNum];//为label数组初始化
            progressbar = new ProgressBar[ComNum];//progressbar为全局变量
            XTF_Outcome = new int[ComNum];//记录每个串口烧写结果
            BGW = new BackgroundWorker[ComNum];//为每个串口创建一个线程
            serialPort = new SerialPort[ComNum];//初始化每个串口
            logInfo = new FileStream[ComNum];//初始化错误日志文件流
            buffer = new string[ComNum];//初始化
            COM_Position = new int[ComNum,2];//初始化位置信息二维数组
            int WFPerRow = (this.Width - 273) / 200;//每行可储放窗口数量,初始化时
            int r = 0, c = 0;//临时变量用于储存行和列数
            missionNum = 0;//初始化为0

            for (int i=0;i<ComNum;i++)
            {
                String tempPath = ErrorLogPath + serialPortArray[i] + "ErrorLog" + ".txt";//为每个串口设置单独的错误日志
                logInfo[i] = new FileStream(tempPath, FileMode.Create);//为每一个串口创建错误日志
                string value = "端口" + serialPortArray[i] + "的错误日志:\r\n";//标题
                byte[] info = new UTF8Encoding(true).GetBytes(value);
                logInfo[i].Write(info, 0, info.Length);
                logInfo[i].Flush();

                buffer[i] = "";

                serialPort[i] = new SerialPort();//创建串口对象
                setPort(i);//对第i个串口进行参数设置

                r = i / WFPerRow;//行数
                c = i % WFPerRow;//列数
                COM_Position[i, 0] = r;
                COM_Position[i, 1] = c;
                Xbutton[i] = new Button();
                Xbutton[i].Size = new Size(100, 50);   //textbox大小                   
                Xbutton[i].Location = new Point(310 + c * 200, 20 + 100 * r);  //textbox坐标,,根据坐标可反向得到i
                Xbutton[i].Name = serialPortArray[i];  //设定控件名称
                Xbutton[i].Text = "开始烧写";//文本内容
                Xbutton[i].Click += new EventHandler(this.XmodemTransfer);//添加事件响应，烧写函数
                panel1.Controls.Add(Xbutton[i]); //把控件加入到panel中

                label[i] = new Label();
                label[i].Size = new Size(40, 30);//label大小
                label[i].Location = new Point(273+c*200 , 40 + 100 * r);//label坐标
                label[i].Text = serialPortArray[i];//label内容
                panel1.Controls.Add(label[i]);//把控件加入到panel1中

                progressbar[i] = new ProgressBar();
                progressbar[i].Size = new Size(150, 20);//textbox大小            
                progressbar[i].Location = new Point(270+c*200, 80 + 100 * r);//textbox坐标,,根据坐标可反向得到i
                progressbar[i].Name = "ProgressBar" + Convert.ToString(i);  //设定控件名称
                panel1.Controls.Add(progressbar[i]); //把控件加入到panel中

                BGW[i] = new BackgroundWorker();//初始化
                BGW[i].WorkerReportsProgress = true;
                BGW[i].WorkerSupportsCancellation = true;
                BGW[i].DoWork += new DoWorkEventHandler(BGWorker_DoWork);//不同线程用的都是同样的方法，区别在于参数
                BGW[i].ProgressChanged += new ProgressChangedEventHandler(BGWorker_ProgressChanged);
                BGW[i].RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGWorker_RunWorkerCompleted);
                
                XTF_Outcome[i] = 0;//初始化为0，烧写成功置1  
            }
        }



        //线程所作操作
        private void BGWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            int id = (int)e.Argument; //串口序号
            bool outcome=true;//传输结果
            int[] result=new int[2];//result[0]表示传输结果，result[1]表示id

            result[1] = id;//传递参数

            Thread.Sleep(100);//使缓冲区完全接收数据

            if (filePath == null)
            {
                AddText(logInfo[id], "未选择传输文件，传输失败");//登记到错误日志
                logInfo[id].Flush();//将缓存写入错误日志
                result[0] = 0;
                e.Result = result;//传输结果
                return;
            }
            //烧写标志
            string XmodemFlag = "ROM:\tUse nor flash.\n\rROM:\tDownload start, enter mode.\n\r";
            //取出最后的55个字符，保证是最新读进来的, 如果字符串长度不足55就直接判断失败
            if (buffer[id].Length >= 55 ? !Regex.IsMatch(buffer[id].Substring(buffer[id].Length - 55), XmodemFlag, RegexOptions.IgnoreCase) : true)//判断是否进入烧写模式
            {
                AddText(logInfo[id], "未正确进入烧写模式，烧写失败！");//记录错误到错误日志
                logInfo[id].Flush();//将缓存写入错误日志
                result[0] = 0;
                e.Result = result;//传输结果
                return;//没有进入烧写模式，直接退出
            }
            
            outcome = XmodemUploadFile(sender,id);//把调用线程的模块传递给Xmodem函数，并进行烧写
            result[0]=outcome?1:0;//表示传输结果，1成功，0失败
            e.Result = result;//传输结果

        }

        //可用于更新UI的progressBar
        private void BGWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //获取id
            int id = (int)e.UserState;
            //修改进度条的显示。
            progressbar[id].Value = e.ProgressPercentage;
            
        }

        //线程完成之后
        private void BGWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBoxButtons messButton1 = MessageBoxButtons.OK;
                DialogResult dr1 = MessageBox.Show(e.Error.ToString(), "发现错误！", messButton1);
                for (int i = 0; i < ComNum; i++) logInfo[i].Flush();//将缓存写入日志s
                this.Close();
            }
            else if (e.Cancelled)
            {
                MessageBoxButtons messButton1 = MessageBoxButtons.OK;
                DialogResult dr1 = MessageBox.Show("传输强制结束", "传输结果", messButton1);
                for(int i=0;i<ComNum;i++) logInfo[i].Flush();//将缓存写入日志
                this.Close();
            }
            else
            {
                int[] result = new int[2];
                result = (int[])e.Result;
                int id = result[1];//解决崩溃问题
                bool flag = (result[0] == 1) ? true : false;
                //修改UI
                if (flag)
                    Xbutton[id].Text = "烧写完毕";
                else
                {
                    MessageBoxButtons messButton1 = MessageBoxButtons.OK;
                    DialogResult dr1 = MessageBox.Show(serialPortArray[id]+"烧写失败,请查看错误日志!", "传输结果", messButton1);
                    Xbutton[id].Text = "烧写失败(点击重新烧写)";
                    Xbutton[id].Enabled = true;//重新使能
                    //变量要全部初始化，为了重新烧写
                    progressbar[id].Value = 0;//重新复位
                }
            }
            //buffer[id] = "";
            missionNum -= 1;//任务完成，数量-1

            /*
            全局统一烧写，烧写全部完成重新置位
            bool flag = true;
            for(int i=0;i<ComNum;i++)
            {
                if(XTF_Outcome[i]==0)
                {
                    flag = false;
                }
            }
            if (flag)//传输完成
            {
                MessageBoxButtons messButton1 = MessageBoxButtons.OK;
                DialogResult dr1 = MessageBox.Show("所有串口传输结束！", "传输结果", messButton1);
                //reset
                //reset时要将所有端口关闭后重新打开，重新运行AutoGeneratingControls
                for(int i=0;i<ComNum;i++)
                {
                    Outcome[i]=0;//reset
                }
            }
            */
        }
        protected override void OnFormClosing(FormClosingEventArgs e)//窗体关闭事件重载
        {
            if(missionNum>0)//有串口还在传输，防止失误关闭
            {
                e.Cancel = true;
            }
            else
            {
                if (MessageBox.Show("你确认要退出该程序吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    for (int i = 0; i < ComNum; i++) logInfo[i].Close();//关闭文件流
                    base.OnFormClosing(e);
                }
                else
                    e.Cancel = true;
            }
        }

        //缩放时自动调整排版
        private void SerialPortW_SizeChanged(object sender, EventArgs e)
        {
            int w = this.Width / 3;//改变后的接受框宽度
            int res = this.Width - w;//剩余宽度
            int WFPerRow = (res - 50 ) / 200;//每行可储放窗口数量,留出50保底
            int r = 0, c = 0;//临时变量用于储存行和列数
            richTextBoxReceive.Width = w;//接受框长度
            button1.Width = w / 2;
            button1.Location = new Point(12 + w / 2 - button1.Width / 2, button1.Location.Y);//置中
            textBox1.Width = w / 2;
            textBox1.Location = new Point(12 + w / 2 - textBox1.Width / 2, textBox1.Location.Y);//置中
            buttonClean.Width = w / 4;
            buttonClean.Location = new Point(12 + w / 2 - buttonClean.Width / 2, buttonClean.Location.Y);//置中

            for (int i = 0; i < ComNum; i++)
            {
                r = (WFPerRow != 0) ? i / WFPerRow : i;//行数，如果一个都放不了就设置为i,每行一个
                c = (WFPerRow != 0) ? i % WFPerRow : 1;//列数，如果一个都放不了就设置为1,每列一个
                COM_Position[i, 0] = r;
                COM_Position[i, 1] = c;
                Xbutton[i].Size = new Size(100, 50);   //textbox大小                   
                Xbutton[i].Location = new Point(w + 80 + c * 200, 20 + 100 * r);  //textbox坐标,,根据坐标可反向得到i

                label[i].Size = new Size(40, 30);//label大小
                label[i].Location = new Point(w + 40 + c * 200, 40 + 100 * r);//label坐标

                progressbar[i].Size = new Size(150, 20);//textbox大小            
                progressbar[i].Location = new Point(w + 40 + c * 200, 80 + 100 * r);//textbox坐标,,根据坐标可反向得到i 
            }
        }
    }
}