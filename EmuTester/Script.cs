using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmuTester
{
    public class Script
    {
        ScriptState _scriptState = ScriptState.None;
        ConnectionWoker _worker;
        System.Timers.Timer retryTimer;
        string previousCommand = string.Empty;
        ManualResetEvent _signalStateChange;
        public Script(ConnectionWoker worker)
        {
            _signalStateChange = new ManualResetEvent(false);
            _worker = worker;
            retryTimer = new System.Timers.Timer(10000);
            retryTimer.Enabled = false;
            retryTimer.AutoReset = false;
            retryTimer.Elapsed += MessageTimer_Elapsed; ;
        }

        private void MessageTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            retryTimer.Stop();
            if(_scriptState == ScriptState.CommandSent)
            {
                Write(previousCommand, () => { });
            }
            if(_scriptState == ScriptState.ACKNSent)
            {
                ;
            }
        }

        public void Execute(string message)
        {
            //string initMessage = "$,1,INIT,1,1,G,16\r";
            string strippedMsg = message.Substring(1, message.Length - 1);
            string chksum = CheckSum.Compute(strippedMsg);
            string command = $"{message}{chksum}\r";
            _worker.PostReadCallback = Process;
            //_worker.Write(command, ()=> { _scriptState = ScriptState.CommandSent; });
            Write(command, () => { _scriptState = ScriptState.CommandSent; });

            while (true)
            {
                _signalStateChange.WaitOne();
                if (_scriptState == ScriptState.ACKNSent)
                {
                    Thread.Sleep(2000);
                    break;
                }
                else
                    _signalStateChange.Reset();
            }
        }

        void Write(string cmd,Action setState)
        {
            previousCommand = cmd;
            _worker.Write(cmd, setState);
            retryTimer.Start();
            _signalStateChange.Set();
        }

        void Process(string cmdstr)
        {
            retryTimer.Stop();
            bool seqNumPresent = false;
            var checkSum = cmdstr.Substring(cmdstr.Length - 1 - 2, 2);
            string strippedCmd = cmdstr.Substring(1, cmdstr.Length - 1 - 3);
            
            

            if (cmdstr.StartsWith("!"))//End Of Execution Message
            {
                Console.WriteLine($"Received End of Execution : {cmdstr}");
                _scriptState = ScriptState.EndOfExecReceived;

                int unit = Convert.ToInt32(cmdstr.Substring(2, 1));
                var fields = cmdstr.Split(',');
                var cmdName = string.Empty;
                if (fields[5].Length == 6)
                {
                    cmdName = fields[4];
                }
                else
                {
                    seqNumPresent = true;
                    cmdName = fields[5];
                }

                //Send ACKN
                string acknmsg = seqNumPresent? $"$,{unit},{fields[2]},ACKN," : $"$,{unit},ACKN,";
                string strippedMsg = acknmsg.Substring(1, acknmsg.Length - 1);
                string chksum = CheckSum.Compute(strippedMsg);
                string command = $"{acknmsg}{chksum}\r";
                //_worker.Write(command, () => { _scriptState = ScriptState.ACKNSent; });
                Write(command, () => { _scriptState = ScriptState.ACKNSent; });
            }
            else if (cmdstr.StartsWith("<"))//Event
            {

            }
            else if (cmdstr.StartsWith("?"))//Error
            {

            }
            else
            {
                Console.WriteLine($"Received Response         : {cmdstr}");
                
                _scriptState = ScriptState.ResponseReceived;
            }

           // _signalStateChange.Set();
        }
    }

    public enum ScriptState
    {
        None,
        CommandSent,
        ResponseReceived,
        EndOfExecReceived,
        ACKNSent,
        ErrorReceived,
        EventReceived
    }

}
