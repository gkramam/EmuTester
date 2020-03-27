using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmuTester
{
    public class LoadPortScript
    {
        ScriptState _scriptState = ScriptState.None;

        LoadPortConnectionWorker _worker;
        ManualResetEvent _signalStateChange;
        BlockingCollection<string> IncomingQ = new BlockingCollection<string>();

        public LoadPortScript(LoadPortConnectionWorker worker)
        {
            _signalStateChange = new ManualResetEvent(false);
            _worker = worker;
            _worker.PostReadCallback = (x) => { IncomingQ.Add(x); };
            _scriptState = ScriptState.None;

            Task.Run(() =>
            {
                foreach (var res in IncomingQ.GetConsumingEnumerable())
                {
                    Process(res);
                }
            });
        }

        public void Execute(string message, bool waitForEventBeforeExecution=false)
        {
            int infCount = 0;
            if(waitForEventBeforeExecution)
            {
                while(true)
                {
                    _signalStateChange.WaitOne();
                    if (_scriptState == ScriptState.INFReceived)
                    {
                        infCount++;
                        if(infCount ==2)
                            break;
                    }
                    else
                        _signalStateChange.Reset();
                }
                _scriptState = ScriptState.None;
            }

            Write(message, () => { _scriptState = ScriptState.CommandSent; });

            while (true)
            {
                _signalStateChange.WaitOne();
                if (_scriptState == ScriptState.ACKReceived)
                {
                    Thread.Sleep(15);
                    break;
                }
                else
                    _signalStateChange.Reset();
            }
        }

        void Write(string cmd, Action setState)
        {
            _worker.Write(cmd, setState);
            _signalStateChange.Set();
        }

        void Process(string cmdstr)
        {
            Console.WriteLine($"Received : {cmdstr}");

            if (cmdstr.Substring(3, 3).Equals("INF"))
                _scriptState = ScriptState.INFReceived;
            else
                _scriptState = ScriptState.ACKReceived;

            _signalStateChange.Set();
        }

        public enum ScriptState
        {
            None,
            CommandSent,
            ACKReceived,
            NAKReceived,
            INFReceived,
            ABSReceived
        }
    }
}
