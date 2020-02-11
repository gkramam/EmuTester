using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public ConnectionWoker() {

            client = new TcpClient();
            client.Connect("localhost", 50100);

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
                while (true)
                {
                    if (client.Connected)
                    {
                        var read = sr.ReadLine();

                        while (string.IsNullOrEmpty(read))
                            read = sr.ReadLine();

                        _postReadCallback(read + '\r');
                        //readQ.Enqueue(read + '\r');
                        //singalToProcess.Set();
                    }
                }
            });

            //Process
            //Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        singalToProcess.WaitOne();
            //        while (readQ.Count > 0)
            //        {
            //            Process(readQ.Dequeue());
            //            if (readQ.Count == 0)
            //                singalToProcess.Reset();
            //        }
            //    }
            //});

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
                            Thread.Sleep(100);
                        }

                        if (writeQ.Count == 0)
                            signalToWrite.Reset();
                    }
                }
            });
        }

        public void Write(string message,Action postWriteCallback)
        {
            Console.WriteLine($"Sending Message : {message}");
            writeQ.Enqueue(message);
            signalToWrite.Set();
            postWriteCallback();
        }

        

    }
}
