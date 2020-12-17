using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Ivi.Visa.Interop;

namespace VNA_Macro_TraceMemory
{
    class Program
    {
        static void Main(string[] args)
        {
            //Status/Error Logging
            Trace.Listeners.Add(new TextWriterTraceListener("logFile.log"));

            //Prepare VISA ecosystem
            ResourceManager rm = new ResourceManager();
            FormattedIO488 vna = new FormattedIO488();
            int readLimit = 1024;

            //Get list of VISA resources from Connection Expert
            string[] resourceList = rm.FindRsrc("?*");
            
            //If no cmd line arg for VISA address is used, assume localhost @ hislip0
            if (args.Length == 0)
            {
                vna.IO = (IMessage)rm.Open("TCPIP0::localhost::hislip0::INSTR");
            }

            //If cmd line arg for VISA address has been added, check it
            else if (args.Length == 1)
            {
                bool validVISAAddress = false;
                //See if cmd line arg is in the list of VISA addresses
                foreach (string str in resourceList)
                {
                    //If there is a match, print it out and set the validVISAAddress flag to true
                    if (args[0].Contains(str))
                    {
                        Console.WriteLine("We have a match.");
                        validVISAAddress = true;
                        break;
                    }
                }

                //Use the cmd line argument if there is a match
                if (validVISAAddress == true)
                {
                    vna.IO = (IMessage)rm.Open(args[0]);
                }
                else
                {
                    Trace.TraceError("Invalid VISA address selected.");
                    System.Environment.Exit(1);
                }
            }
            vna.IO.Timeout = 5000;

            //Get list of active channels from VNA
            vna.IO.WriteString("system:channels:catalog?");
            string rawChannels = vna.IO.ReadString(readLimit);

            //Convert single string list of channels to individual strings
            string[] channels = rawChannels.Trim().Replace("\"", string.Empty).Split(',');

            //Send Data -> Memory for each trace in each channel
            for (int i = 0; i < channels.Length; i++)
            {
                //Get list of traces for a given channel
                vna.IO.WriteString($"system:measure:catalog? {channels[i]}");
                string rawTraces = vna.IO.ReadString(readLimit);
                string[] traces = rawTraces.Trim().Replace("\"", string.Empty).Split(',');
                //Send Data -> Memory for each trace in the channel
                for (int j = 0; j < traces.Length; j++)
                {
                    vna.IO.WriteString($"calculate:measure{traces[j]}:math:memorize");
                }
            }

            //Get list of active windows from VNA
            vna.IO.WriteString("display:catalog?");
            string rawWindows = vna.IO.ReadString(readLimit);

            //Convert single string list of windows to individual strings
            string[] windows = rawWindows.Trim().Replace("\"", string.Empty).Split(',');

            //Activate Data and Memory for each trace in each window
            for (int k = 0; k < windows.Length; k++)
            {
                //Get array of traces per window
                vna.IO.WriteString($"display:window{windows[k]}:catalog?");
                string rawWinTraces = vna.IO.ReadString(readLimit);
                string[] winTraces = rawWinTraces.Trim().Replace("\"", string.Empty).Split(',');

                //Ensure each trace is turned on and activate the memory trace 
                for (int l = 0; l < winTraces.Length; l++)
                {
                    vna.IO.WriteString($"display:window{windows[k]}:trace{winTraces[l]}:state on");
                    vna.IO.WriteString($"display:window{windows[k]}:trace{winTraces[l]}:memory:state on");
                }
            }

            //Array.ForEach(channels, Console.WriteLine);
            //Console.ReadLine();
        }
    }
}
