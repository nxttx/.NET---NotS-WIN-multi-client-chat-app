﻿using System;
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
        // Stap 3:
        TcpClient tcpClient;
        NetworkStream networkStream;
        Thread thread;

        // Stap 4: 
        protected delegate void UpdateDisplayDelegate(string message);

        public Form1()
        {
            InitializeComponent();
        }

        // Stap 5:
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

        // Stap 6:
        private void btnListen_Click(object sender, EventArgs e)
        {
            try
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Any, 9000);
                tcpListener.Start();
                
                AddMessage("Listening for client.");

                tcpClient = tcpListener.AcceptTcpClient();
                thread = new Thread(new ThreadStart(ReceiveData));
                thread.Start();
            }
            catch (SocketException exception)
            {
                AddMessage("Foutmelding: er is al een server actief op: " + IPAddress.Any );
                Console.WriteLine("Exception: ", exception);
            }
            catch (Exception exception)
            {
                AddMessage("Foutmelding: Er is iets fout gegaan: " + exception);
                Console.WriteLine("Exception: ", exception);
            }
        }

        // Stap 7:
        private void ReceiveData()
        {
            txtBufferSize.Enabled = false;
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

                if (message == "bye")
                    break;

                AddMessage(message);
            }

            // Verstuur een reactie naar de client (afsluitend bericht)
            buffer = Encoding.ASCII.GetBytes("bye");
            networkStream.Write(buffer, 0, buffer.Length);

            // cleanup:
            networkStream.Close();
            tcpClient.Close();

            AddMessage("Connection closed");
            txtBufferSize.Enabled = true;
        }

        // Stap 8:
        private void btnConnectWithServer_Click_1(object sender, EventArgs e)
        {
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
            }
            catch (Exception exception)
            {
                AddMessage("Foutmelding: Er is iets fout gegaan: " + exception);
                Console.WriteLine("Exception: ", exception);
            }
        }

        // Stap 9:
        private void btnSendMessage_Click(object sender, EventArgs e)
        {
            string message = txtMessageToBeSend.Text;

            byte[] buffer = Encoding.ASCII.GetBytes(message);
            try
            {
                networkStream.Write(buffer, 0, buffer.Length);

                AddMessage(message);
                txtMessageToBeSend.Clear();
                txtMessageToBeSend.Focus();
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