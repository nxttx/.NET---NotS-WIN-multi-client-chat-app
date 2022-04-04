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

namespace _01_ClientServerChat
{
    public partial class Form1 : Form
    {
        TcpClient tcpClient;
        NetworkStream networkStream;
        Thread thread;
        
        protected delegate void UpdateDisplayDelegate(string message);
        protected delegate void ToggleFieldsDelegate();
        private ToggleFieldsDelegate toggleFields;

        public Form1()
        {
            FormClosing += (o, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    Disconnect_Click(new object(), new EventArgs());
                    Application.Exit();
                }
            };
            InitializeComponent();
            toggleFields = () =>
            {
                txtBufferSize.Enabled = !txtBufferSize.Enabled;
                txtServerName.Enabled = !txtServerName.Enabled;
                txtChatServerIP.Enabled = !txtChatServerIP.Enabled;
                btnConnectWithServer.Enabled = !btnConnectWithServer.Enabled;
                Disconnect.Enabled = !Disconnect.Enabled;
            };
        }
        
        private void AddMessage(string message)
        {
            if (listChats.InvokeRequired)
            {
                listChats.Invoke(new UpdateDisplayDelegate(UpdateDisplay), new object[] { message });
            }
            else
            {
                UpdateDisplay(message);
            }
        }

        private void UpdateDisplay(string message)
        {
            listChats.Items.Add(message);
        }
        
        private void ReceiveData()
        {
            int bufferSize;
            int ignoreMe;
            bool succes = int.TryParse(txtBufferSize.Text, out ignoreMe);
            if (succes && int.Parse(txtBufferSize.Text) >=1 && int.Parse(txtBufferSize.Text) <=25565 )            {
                bufferSize = int.Parse(txtBufferSize.Text);
            }
            else 
            {
                bufferSize = 1024;
                AddMessage("Foutmelding: Buffersize moet een positief interger nummer zijn en kleiner dan 25565. We hebben de buffer size aangepast naar 1024.");
                txtBufferSize.Text = "1024";
            }
            StringBuilder SB = new StringBuilder();
            byte[] buffer = new byte[bufferSize];

            networkStream = tcpClient.GetStream();
            AddMessage("Connected!");

            while (true)
            {

                string partMsg = "";
                try
                {
                    while (networkStream.DataAvailable){
                        int readBytes = networkStream.Read(buffer, 0, buffer.Length);
                        partMsg = Encoding.ASCII.GetString(buffer, 0, readBytes);
                        SB.Append(partMsg);
                        // clear buffer:
                        buffer = new byte[bufferSize];
                    
                    }
                }
                catch (ObjectDisposedException e)
                {
                    /*
                     * due to an race condition,
                     * we will catch if the object doesnt exist anymore.
                     * And abort the thread, clearing everything 
                     */
                    thread.Abort();
                    AddMessage("Connection closed");
                }
                catch (Exception exception)
                {
                    AddMessage("Foutmelding: Er is iets fout gegaan: " + exception);
                    Console.WriteLine("Exception: ", exception);
                }

                if (SB.ToString().Contains("SERVER SAYS BYE"))
                    break;
                
                if (SB.ToString() != "")
                {
                    AddMessage(SB.ToString());
                    SB.Clear();
                }
            }

            // Verstuur een reactie naar de client (afsluitend bericht)
            buffer = Encoding.ASCII.GetBytes("[" + txtServerName.Text+ "]: bye");
            networkStream.Write(buffer, 0, buffer.Length);

            // cleanup:
            networkStream.Close();
            tcpClient.Close();

            AddMessage("Connection closed");
            toggleFields();
        }
        
        private void btnConnectWithServer_Click_1(object sender, EventArgs e)
        {
            if (txtServerName.Text == "")
            {
                AddMessage("Foutmelding: Je moet een gebruikersnaam invullen.");
                return;
            }
            toggleFields();
            AddMessage("Connecting...");
            try
            {
                tcpClient = new TcpClient(txtChatServerIP.Text, 9000);
                thread = new Thread(new ThreadStart(ReceiveData));
                thread.Start();
            }
            catch (SocketException exception)
            {
                AddMessage("Foutmelding: We konden op: " + txtChatServerIP.Text + ". Geen server vinden.");
                Console.WriteLine("Exception: ", exception);
                toggleFields();
            }
            catch (Exception exception)
            {
                AddMessage("Foutmelding: Er is iets fout gegaan: " + exception);
                Console.WriteLine("Exception: ", exception);
                toggleFields();
            }
        }
        
        private void btnSendMessage_Click(object sender, EventArgs e)
        {
            string message = "[" + txtServerName.Text+ "]: " + txtMessageToBeSend.Text;

            byte[] buffer = Encoding.ASCII.GetBytes(message);
            try
            {
                networkStream.Write(buffer, 0, buffer.Length);

                // AddMessage(message);
                txtMessageToBeSend.Clear();
                txtMessageToBeSend.Focus();
            }
            catch (NullReferenceException exception)
            {
                AddMessage("Foutmelding: Je moet wel verbonden zijn met een server om een bericht te verzenden.");
                Console.WriteLine("Exception: ", exception);
            }
            catch (ObjectDisposedException exception)
            {
                AddMessage("Foutmelding: Je moet wel verbonden zijn met een server om een bericht te verzenden.");
                Console.WriteLine("Exception: ", exception);
            }
            catch (Exception exception)
            {
                AddMessage("Foutmelding: Er is iets fout gegaan: " + exception);
                Console.WriteLine("Exception: ", exception);
            }

        }

        private void Disconnect_Click(object sender, EventArgs e)
        {
            string message = "CLIENT SAYS GOODBYE";

            byte[] buffer = Encoding.ASCII.GetBytes(message);
            try
            {
                networkStream.Write(buffer, 0, buffer.Length);
                
                txtMessageToBeSend.Clear();
                txtMessageToBeSend.Focus();
                // cleanup:
                networkStream.Close();
                tcpClient.Close();

                toggleFields();
                
            }
            catch (NullReferenceException exception)
            {
                AddMessage("Foutmelding: Je moet wel verbonden zijn met een server om een bericht te verzenden.");
                Console.WriteLine("Exception: ", exception);
            }
            catch (Exception exception)
            {
                AddMessage("Foutmelding: Er is iets fout gegaan: " + exception);
                Console.WriteLine("Exception: ", exception);
            }
        }
    }
}
