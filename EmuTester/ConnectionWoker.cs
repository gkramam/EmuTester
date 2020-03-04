using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace EmuTester
{
    public class ConnectionWoker
    {
        TcpClient client;
        Action<string> _postReadCallback = null;
        public Action<string> PostReadCallback { get { return _postReadCallback; } set { _postReadCallback = value; } }

        //Queue<string> writeQ = new Queue<string>();

        BlockingCollection<string> writeQ = new System.Collections.Concurrent.BlockingCollection<string>();

        Timer messageTimer;
        StringBuilder commandString = new StringBuilder();
        bool startDetected = false;

        private bool _stop = false;
        public void Stop()
        {
            _stop = true;
        }
        public ConnectionWoker(int port) {

            client = new TcpClient();
            client.Connect("localhost", port);
            client.NoDelay = true;

            messageTimer = new Timer(20);
            messageTimer.Enabled = false;
            messageTimer.AutoReset = false;
            messageTimer.Elapsed += MessageTimer_Elapsed;

        }

        private void MessageTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //messageTimer.Stop();
            //commandString = new StringBuilder();
            //startDetected = false;
        }

        public void Start()
        {
            Stream s = client.GetStream();
            StreamReader sr = new StreamReader(s, Encoding.ASCII);
            StreamWriter sw = new StreamWriter(s, Encoding.ASCII);
            sw.AutoFlush = true;

            //Read
            //Thread read = new Thread(() =>
            Task.Run(() =>
            {
                while (!_stop && client.Connected)
                {
                    if (client.Client.Poll(-1, SelectMode.SelectRead))
                    {
                        var amt = client.ReceiveBufferSize;

                        char[] readBuffer = new char[amt];
                        var length = sr.Read(readBuffer, 0, amt);
                        readBufferQ.Add(readBuffer.Take(length).ToArray());
                    }
                }
                sr.Close();
                client.Dispose();
            });
            //read.Priority = ThreadPriority.AboveNormal;
            //read.IsBackground = true;
            //read.Start();

            //Write
            //Thread write = new Thread(() =>
            Task.Run(() =>
            {
                //while(!_stop)
                {
                    foreach(var msg in writeQ.GetConsumingEnumerable())
                    {
                        if (client.Client.Poll(-1, SelectMode.SelectWrite))
                        {
                            sw.Write(msg);
                        }
                        if (_stop)
                            break;
                    }
                }

                sw.Close();
                writeQ.Dispose();
                client.Dispose();
            });
            //write.Priority = ThreadPriority.AboveNormal;
            //write.IsBackground = true;
            //write.Start();

            //Thread process = new Thread(() =>
            Task.Run(() =>
            { ProcessReadBufer(); });
            //process.Priority = ThreadPriority.AboveNormal;
            //process.IsBackground = true;
            //process.Start();
        }

        BlockingCollection<char[]> readBufferQ = new BlockingCollection<char[]>();
        void ProcessReadBufer()
        {
            foreach (var buffer in readBufferQ.GetConsumingEnumerable())
            {
                foreach (var read in buffer)
                {
                    if ((read == '$' || read == '!' || read == '<' || read == '?') && !startDetected)
                    {
                        startDetected = true;
                        messageTimer.Start();
                        commandString.Append(read);
                    }
                    else if ((read == '$' || read == '!' || read == '<' || read == '?') && startDetected)
                    {
                        commandString = new StringBuilder();
                        messageTimer.Stop();

                        startDetected = true;
                        messageTimer.Start();
                        commandString.Append(read);
                    }
                    else if (read == '\r')
                    {
                        //Console.Write('R');
                        if (startDetected)
                        {
                            messageTimer.Stop();
                            commandString.Append(read);
                            var cmd = commandString.ToString();
                            //readCommandQ.Enqueue(cmd);
                            PostReadCallback(cmd);
                            commandString = new StringBuilder();
                            startDetected = false;
                        }
                        else
                        {
                            commandString = new StringBuilder();
                            messageTimer.Stop();
                        }
                    }
                    else
                    {
                        messageTimer.Stop();
                        commandString.Append(read);
                        messageTimer.Start();
                    }
                }
            }
        }

        public void Write(string message,Action postWriteCallback)
        {
            //Console.WriteLine($"Sending Message           : {message}");
            Program.ConsoleQ.Add($"Sending Message           : {message}");
            writeQ.Add(message);
            postWriteCallback();
        }

        

    }
}
