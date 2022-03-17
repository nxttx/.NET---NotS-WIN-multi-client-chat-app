using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _02_ChatServer
{
    public partial class Form1 : Form
    {
        Thread thread;
        protected delegate void UpdateDisplayDelegate(string message);

        protected delegate void ToggleFieldsDelegate();

        protected delegate bool StopServerDeligate(bool stop);
        
        protected delegate String GlobalMessageDelgate(String newMessage);

        private ToggleFieldsDelegate toggleFields;
        private StopServerDeligate StopServer;
        private GlobalMessageDelgate GlobalMessage;
        private bool shouldServerStop;
        private String lastMessage = "";
        public Form1()
        {
            toggleFields = () =>
            {
                btnStop.Enabled = !btnStop.Enabled;
                btnStart.Enabled = !btnStart.Enabled;
                txtServerIP.Enabled = !txtServerIP.Enabled;
                txtBufferSize.Enabled = !txtBufferSize.Enabled;
            };

            StopServer = (bool stop) =>
            {
                if (stop)
                    shouldServerStop = true;
                
                return shouldServerStop;
            };

            GlobalMessage = (String newMessage) =>
            {
                if (newMessage != "")
                {
                    lastMessage = newMessage;
                }

                return lastMessage;
            };
            InitializeComponent();
        }

        private void AddMessage(string message)
        {
            if (listMessages.InvokeRequired)
            {
                listMessages.Invoke(new UpdateDisplayDelegate(UpdateDisplay), new object[] { message });
            }
            else
            {
                UpdateDisplay(message);
            }
        }

        private void UpdateDisplay(string message)
        {
            listMessages.Items.Add(message);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            shouldServerStop = false;
            toggleFields();
            thread = new Thread(new ThreadStart(() =>
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Any, 9000);
                try
                {
                    
                    tcpListener.Start();

                    AddMessage("Listening for client.");
                    // check this for errors: It is probably the reason the server doesnt close: 
                    // it waits on an new client. 
                    while (!StopServer(false))
                    {
                        if (tcpListener.Pending())
                        {
                            TcpClient tcpClient = tcpListener.AcceptTcpClient();
                            ThreadPool.QueueUserWorkItem(ReceiveData, tcpClient);
                        }
                    }
                    
                    tcpListener.Stop();
                }
                catch (SocketException exception)
                {
                    AddMessage("Foutmelding: er is al een server actief op: " + IPAddress.Any);
                    Console.WriteLine("Exception: ", exception);
                    tcpListener.Stop();
                    toggleFields();
                    
                }
                catch (Exception exception)
                {
                    AddMessage("Foutmelding: Er is iets fout gegaan: " + exception);
                    Console.WriteLine("Exception: ", exception);
                    tcpListener.Stop();
                    toggleFields();
                }
            }));

            thread.Start();
        }
        
        private void ReceiveData(Object obj)
        {
            TcpClient tcpClient = (TcpClient) obj;
            int bufferSize;
            int ignoreMe;
            bool succes = int.TryParse(txtBufferSize.Text, out ignoreMe);
            if (succes && int.Parse(txtBufferSize.Text) >=1 && int.Parse(txtBufferSize.Text) <=25565 )
            {
                bufferSize = int.Parse(txtBufferSize.Text);
            }
            else 
            {
                bufferSize = 1024;
                AddMessage("Foutmelding: Buffersize moet een positief interger nummer zijn en kleiner dan 25565. We hebben de buffer size aangepast naar 1024.");
                txtBufferSize.Text = "1024";
            }
            byte[] buffer = new byte[bufferSize];

            StringBuilder SB = new StringBuilder();
            NetworkStream networkStream = tcpClient.GetStream();
            AddMessage("Connected!");
            String oldMessage = "";

            while (!StopServer(false) )
            {
                string partMsg = "";
                while (networkStream.DataAvailable){
                    int readBytes = networkStream.Read(buffer, 0, buffer.Length);
                    partMsg = Encoding.ASCII.GetString(buffer, 0, readBytes);
                    SB.Append(partMsg);
                    // clear buffer:
                    buffer = new byte[bufferSize];
                    
                }

                if (oldMessage != GlobalMessage(""))
                {
                    oldMessage = GlobalMessage("");
                    buffer = Encoding.ASCII.GetBytes(GlobalMessage(""));
                    networkStream.Write(buffer, 0, buffer.Length);
                }

                if (SB.ToString() != "")
                {
                    if (SB.ToString() == "CLIENT SAYS GOODBYE")
                    {
                        break;
                    }
                    GlobalMessage(SB.ToString());
                    AddMessage(SB.ToString());
                    SB.Clear();
                }
            }

            if (StopServer(false))
            {
                buffer = Encoding.ASCII.GetBytes("SERVER SAYS BYE");
                networkStream.Write(buffer, 0, buffer.Length);
                AddMessage("closed server");
            }
            
            // cleanup:
            networkStream.Close();
            tcpClient.Close();
            
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            //delegate that tells the server that it should stop.
            StopServer(true);
            toggleFields();
            
        }
    }
}
