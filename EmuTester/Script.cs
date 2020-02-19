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
        System.Timers.Timer messageTimer;
        string previousCommand = string.Empty;

        public Script(ConnectionWoker worker)
        {
            _worker = worker;
            messageTimer = new System.Timers.Timer(5000);
            messageTimer.Enabled = false;
            messageTimer.AutoReset = false;
            messageTimer.Elapsed += MessageTimer_Elapsed; ;
        }

        private void MessageTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            messageTimer.Stop();
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
            
            while (_scriptState != ScriptState.ACKNSent)
            {
                Thread.Sleep(1000);
            }

            //Thread.Sleep(1000);
        }

        void Write(string cmd,Action setState)
        {
            previousCommand = cmd;
            _worker.Write(cmd, setState);
            //messageTimer.Start();
        }

        void Process(string cmdstr)
        {
            messageTimer.Stop();
            bool seqNumPresent = false;
            var checkSum = cmdstr.Substring(cmdstr.Length - 1 - 2, 2);
            string strippedCmd = cmdstr.Substring(1, cmdstr.Length - 1 - 3);
            int unit = Convert.ToInt32(cmdstr.Substring(2, 1));
            var fields = cmdstr.Split(',');
            var cmdName = string.Empty;
            if (fields[2].Length == 2)
            {
                seqNumPresent = true;
                cmdName = fields[3];
            }
            else
                cmdName = fields[2];
            

            if (cmdstr.StartsWith("!"))//End Of Execution Message
            {
                Console.WriteLine($"Received End of Execution : {cmdstr}");
                _scriptState = ScriptState.EndOfExecReceived;
                //Send ACKN
                string acknmsg = seqNumPresent? $"$,{unit},{fields[2]},ACKN," : $"$,{unit},ACKN,";
                string strippedMsg = acknmsg.Substring(1, acknmsg.Length - 1);
                string chksum = CheckSum.Compute(strippedMsg);
                string command = $"{acknmsg}{chksum}\r";
                _worker.Write(command, () => { _scriptState = ScriptState.ACKNSent; });
            }
            else if (cmdstr.StartsWith("<"))//Event
            {

            }
            else if (cmdstr.StartsWith("?"))//Error
            {

            }
            else
            {
                _scriptState = ScriptState.ResponseReceived;
                Console.WriteLine($"Received Response         : {cmdstr}");
                //Normal Response $
            }
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
