using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
        TcpClient tcpClient;
        NetworkStream networkStream;
        Thread thread;
        protected delegate void UpdateDisplayDelegate(string message);

        protected delegate void ToggleFieldsDelegate();

        protected delegate bool StopServerDeligate(bool stop);

        private ToggleFieldsDelegate toggleFields;
        private StopServerDeligate StopServer;
        private bool ShouldServerStop = false; 
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
                    ShouldServerStop = true;
                
                return ShouldServerStop;
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
            ShouldServerStop = false;
            toggleFields();
            thread = new Thread(new ThreadStart(() =>
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Any, 9000);
                try
                {
                    
                    tcpListener.Start();

                    AddMessage("Listening for client.");

                    tcpClient = tcpListener.AcceptTcpClient();
                    ReceiveData();
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
        
        private void ReceiveData()
        {
            int bufferSize;
            int ignoreMe;
            bool succes = int.TryParse(txtBufferSize.Text, out ignoreMe);
            if (succes)
            {
                bufferSize = int.Parse(txtBufferSize.Text);
            }
            else 
            {
                bufferSize = 1024;
                AddMessage("Foutmelding: Buffersize moet een interger nummer zijn. We hebben de buffer size aangepast naar 1024.");
                txtBufferSize.Text = "1024";
            }
            string message = "";
            byte[] buffer = new byte[bufferSize];

            networkStream = tcpClient.GetStream();
            AddMessage("Connected!");

            while (true)
            {
                int readBytes = networkStream.Read(buffer, 0, bufferSize);
                message = Encoding.ASCII.GetString(buffer, 0, readBytes);
                
                AddMessage(message);

                if (StopServer(false))
                    break;
            }
            buffer = Encoding.ASCII.GetBytes("SERVER SAYS BYE");
            networkStream.Write(buffer, 0, buffer.Length);

            // cleanup:
            networkStream.Close();
            tcpClient.Close();
            // thread.Abort();

            AddMessage("closed server");
            
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopServer(true);
            thread = new Thread(new ThreadStart(() =>
            {
                byte[] buffer = new byte[int.Parse(txtBufferSize.Text)];
                buffer = Encoding.ASCII.GetBytes("SERVER SAYS BYE");
                networkStream.Write(buffer, 0, buffer.Length);
            }));
            thread.Start();
            toggleFields();
        }
    }
}
