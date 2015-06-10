using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using System.IO;

namespace 串口工具
{
    public partial class Form1 : Form
    {
        Thread thSend, thRead;
        string statusStr = "Closed ...";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tb_send.Focus();
            tb_send.TabIndex = 0;
            tb_send.TabStop = true;
            thRead = new Thread( new ThreadStart(thReadThread));
            bt_send.Enabled = false; //禁能发送按钮
        }

        bool uartIsOpen = false;
        private void bt_open_Click(object sender, EventArgs e)
        {
            //关闭接口
            if (hSerialPort.IsOpen)
            {
                //1、操作处理
                uartIsOpen = false;
                bt_send.Enabled = false;
                bt_open.Text = "打开串口";
                hSerialPort.Close();
                statusStr = "Closed...";
                timer_send.Enabled = false; //定时发送功能必须停止
                bt_send.Enabled = false;    //发送按钮 禁止
                bt_send.Text = "发送";      //发送按钮 内容复位
                
                //2、线程处理
                thRead.Suspend();//接收线程挂起
                if (cb_record.Checked)
                {
                    if (stwHandle != null)
                    {
                        stwHandle.Close();
                    }
                }
                                
                //3、显示处理
                //关闭串口后 修改配置
                text_baud.Enabled = true;
                text_com.Enabled = true;
                text_parity.Enabled = true;
                text_stop.Enabled = true;
                text_data.Enabled = true;                                                
            }
            else
            {
                uartIsOpen = true;
                hSerialPort.PortName = "COM" + text_com.Value.ToString();

                int TempInt = 0;
                if (int.TryParse(text_baud.Text, out  TempInt))
                {
                    hSerialPort.BaudRate =  TempInt;
                }
                else
                {
                    MessageBox.Show("波特率数据格式错误！");
                }

                if (int.TryParse(text_data.Text, out  TempInt))
                {
                    hSerialPort.DataBits = TempInt;
                }
                else
                {
                    MessageBox.Show("数据位格式错误！");
                }

                switch (text_parity.Text)
                {
                    case "无":
                        hSerialPort.Parity = System.IO.Ports.Parity.None;
                        break;

                    case "奇校验":
                        hSerialPort.Parity = System.IO.Ports.Parity.None;
                        break;

                    case "偶校验":
                        hSerialPort.Parity = System.IO.Ports.Parity.None;
                        break;

                    default:
                        MessageBox.Show("校验信息错误！");
                        break;
                }

                switch (text_stop.Text)
                {
                    case "1":
                        hSerialPort.StopBits = System.IO.Ports.StopBits.One;
                        break;

                    case "1.5":
                        hSerialPort.StopBits = System.IO.Ports.StopBits.OnePointFive;
                        break;

                    case "2":
                        hSerialPort.StopBits = System.IO.Ports.StopBits.Two;
                        break;

                    default:
                        MessageBox.Show("校验信息错误！");
                        break;
                }

                try
                {
                    hSerialPort.Open();
                }
                catch(Exception ee)
                {
                    MessageBox.Show(ee.ToString());
                    return;
                }

                bt_open.Text = "关闭串口";
                bt_send.Enabled = true; //使能发送按钮

                statusStr = "COM"+text_com.Value.ToString();
                statusStr += ", Baudrate " + text_baud.Text.ToString();
                statusStr += ", Databits " + text_data.Text.ToString();
                statusStr += ", Parity " + text_parity.Text.ToString();
                statusStr += ", Stopbits " + text_stop.Text.ToString();                

                if (thRead.ThreadState == ThreadState.Unstarted)
                {
                    thRead.Start();                    
                }
                else
                {
                    thRead.Resume();
                }

                if ( (cb_record.Checked) )
                {
                    stwHandle = new StreamWriter(tb_record.Text, true, Encoding.Unicode);
                    stwHandle.Write("\r\n" + DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString() + "\r\n");
                }
                else
                {
                    stwHandle = null;
                }

                //
                text_baud.Enabled = false;
                text_com.Enabled = false;
                text_parity.Enabled = false;                
                text_stop.Enabled = false;
                text_data.Enabled = false;
            }

            //更新status
            tssl_status.Text = statusStr;
        }

        //接收数据线程
        string readStr = "";
        StreamWriter stwHandle; //写入文件句柄
        void thReadThread()
        {
            string TempStr = "";
            while (true)
            {
                if (uartIsOpen)
                {
                    TempStr = hSerialPort.ReadExisting();
                }

                if (TempStr!="")
                {
                    readStr += TempStr;
                    if (stwHandle != null)
                    {
                        stwHandle.Write(TempStr);
                    }
                }
            }        
        }

        private void timer_ref_Tick(object sender, EventArgs e)
        {
            tb_read.Text = readStr;
            tssl_rx.Text = "Rx: " + readStr.Length.ToString();
            try
            {
                if (stwHandle != null)
                {
                    stwHandle.Flush();
                }
            }catch(Exception ee)
            {
            }
        }
        
        string strSend = "";
        UInt32 SendLength = 0;
        private void bt_send_Click(object sender, EventArgs e)
        {
            strSend = tb_send.Text;
            if (cb_r.Checked)
            {
                strSend += "\r";
            }
            if (cb_n.Checked)
            {
                strSend += "\n";
            }

            if (strSend == "")
            {
                MessageBox.Show("发送数据不能为空！");
            }

            //自动发送
            if (cb_autoSend.Checked)
            {
                timer_send.Interval = 100;
                int interval = 100;
                if (int.TryParse(tb_interval.Text, out  interval))
                {
                    timer_send.Interval = interval;    
                }

                timer_send.Enabled = !timer_send.Enabled;  //取反

                if (timer_send.Enabled)
                {
                    bt_send.Text = "关闭 自动发送";
                }
                else
                {
                    bt_send.Text = "发送";
                }
            }
            //单次发送
            else
            {                 
                hSerialPort.Write(strSend);

                SendLength += (UInt32)(strSend.Length);
                tssl_tx.Text = "Tx: " + SendLength.ToString();
            }
        }


        //自动发送定时器
        private void timer_send_Tick(object sender, EventArgs e)
        {
            try
            {
                hSerialPort.Write(strSend);

                SendLength += (UInt32)(strSend.Length);
                tssl_tx.Text = "Tx: " + SendLength.ToString();
            }
            catch (Exception ee)
            { }
        }


        private void bt_clear_Click(object sender, EventArgs e)
        {
            readStr = new string(' ',0);
            tb_read.Text = "";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            try
            {
                //如果线程挂起的 直接abort不成功 会导致显现依旧在运行！
                if (thRead.ThreadState == ThreadState.Suspended)
                {
                    thRead.Resume();
                }

                thRead.Abort();
                thRead.Join();

                if ((cb_record.Checked) && (stwHandle != null))
                {
                    stwHandle.Close();
                }
                hSerialPort.Close();

            }catch(Exception ee)
            {            
            }
           
        }

        private void button1_Click(object sender, EventArgs e)
        {
            tb_send.Text = "";
            SendLength = 0;
            tssl_tx.Text = "Tx: 0";
        }

        private void label6_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "设置参数可以手动修改，实际是否可以设置成功取决于串口设备和其驱动！比如：使用CP2102的USB转串口可以设置1M或2M的波特率！电脑自带的串口就不可以！",
                "帮助");
        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            
        }



        

       

   
    }
}
