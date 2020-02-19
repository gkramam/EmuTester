using System;
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

        Queue<string> readQ = new Queue<string>();
        Queue<string> writeQ = new Queue<string>();

        ManualResetEvent singalToProcess = new ManualResetEvent(false);
        ManualResetEvent signalToWrite = new ManualResetEvent(false);

        Timer messageTimer;
        StringBuilder commandString = new StringBuilder();
        bool startDetected = false;

        public ConnectionWoker(int port) {

            client = new TcpClient();
            client.Connect("localhost", port);

            messageTimer = new Timer(20);
            messageTimer.Enabled = false;
            messageTimer.AutoReset = false;
            messageTimer.Elapsed += MessageTimer_Elapsed;

        }

        private void MessageTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            messageTimer.Stop();
            commandString = new StringBuilder();
            startDetected = false;
        }

        public void Start()
        {
            Stream s = client.GetStream();
            StreamReader sr = new StreamReader(s, Encoding.ASCII);
            StreamWriter sw = new StreamWriter(s, Encoding.ASCII);
            sw.AutoFlush = true;

            //Read
            Task.Run(() =>
            {
                while (client.Connected)
                {
                    var amt = client.Available;

                    if (amt <= 0)
                        continue;

                    char read = (char)sr.Read();

                    if ((read == '$' || read == '!' || read == '<' || read== '?')&& !startDetected)
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
                            Task.Run(()=> { PostReadCallback(cmd); });
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
            });

            //Write
            Task.Run(() => 
            {
                while(true)
                {
                    signalToWrite.WaitOne();
                    while(writeQ.Count>0)
                    {
                        var msg = writeQ.Dequeue();
                        var chars = msg.ToCharArray();
                        foreach (char c in chars)
                        {
                            sw.Write(c);
                            //sw.Flush();
                            Thread.Sleep(5);//100 was working without task
                        }

                        if (writeQ.Count == 0)
                            signalToWrite.Reset();
                    }
                }
            });
        }

        public void Write(string message,Action postWriteCallback)
        {
            Console.WriteLine($"Sending Message           : {message}");

            writeQ.Enqueue(message);
            signalToWrite.Set();
            postWriteCallback();
        }

        

    }
}
