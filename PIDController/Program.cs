using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using CsvHelper.Configuration;
using System.IO.Ports;

namespace PIDController
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = "setting.json";
            string c = Path.DirectorySeparatorChar.ToString();
            string filepath = Directory.GetCurrentDirectory() + c + filename;
            var jobj = JObject.Parse(File.ReadAllText(filepath));
            int SamplingSpan = int.Parse(jobj["SAMPLING_SPAN"].ToString());
            int TimeSpan = int.Parse(jobj["TIMESPAN"].ToString());
            int mode_of_feedforward = int.Parse(jobj["FEEDFORWARDMODE"].ToString());
            double GAIN_P = double.Parse(jobj["GAIN_P"].ToString());
            double GAIN_I = double.Parse(jobj["GAIN_I"].ToString());
            double GAIN_D = double.Parse(jobj["GAIN_D"].ToString());
            double OUTPUT_MAX = double.Parse(jobj["OUTPUT_MAX"].ToString());
            double OUTPUT_MIN = double.Parse(jobj["OUTPUT_MIN"].ToString());
            bool debug = bool.Parse(jobj["DEBUG"].ToString());
            bool feedback_mode = bool.Parse(jobj["FEEDBACK"].ToString());

            PidController pid = new PidController(GAIN_P, GAIN_I, GAIN_D, OUTPUT_MAX, OUTPUT_MIN);

            SerialPort LKG5000 = new SerialPort(jobj["PORT_NAME"].ToString(), int.Parse(jobj["BAURATE"].ToString()), Parity.None, 8, StopBits.One);
            LKG5000.Open();
            LKG5000.Write("MS,01\r");
            LKG5000.NewLine = "\r";
            string returnstring = "";
            if (debug)
            {
                returnstring = "MS,01, 1234.56";
            }
            else
            {
                returnstring = LKG5000.ReadLine();
            }
            double current_value = double.Parse(returnstring.Substring(int.Parse(jobj["START_INDEX"].ToString()), int.Parse(jobj["COUNT"].ToString())));
            LKG5000.Close();

            pid.SetPoint = double.Parse(jobj["SET_POINT"].ToString());
            pid.ProcessVariable = current_value;
            pid.ProcessVariableLast = double.Parse(jobj["LAST_VALUE"].ToString());
            //  TODO    : change LAST_VALUE 

            string logfilepath = Directory.GetCurrentDirectory() + /*c + ".." + c + "Log" +*/ c + jobj["LOG_FILE_NAME"].ToString();

            TimeSpan pastTimeFromLastUpdate = new TimeSpan(0, 0, 0, SamplingSpan);
            double output;

            if (feedback_mode)
            {
                output = pid.ControlVariable(jobj, pastTimeFromLastUpdate);
            }
            else
            {
                if (mode_of_feedforward == 1)
                {
                    output = pid.ProcessVariableLast + (pid.OutputMax - pid.OutputMin) / TimeSpan * SamplingSpan;
                }
                else if (mode_of_feedforward == 2)
                {
                    output = pid.ProcessVariableLast - (pid.OutputMax - pid.OutputMin) / TimeSpan * SamplingSpan;
                }
                else
                {
                    output = pid.OutputMin;
                }
            }

            System.IO.StreamWriter sw_csv = new System.IO.StreamWriter(logfilepath,true,System.Text.Encoding.GetEncoding("shift_jis"));
            int now_in_second = DateTime.Now.Minute * 60 + DateTime.Now.Second;
            sw_csv.Write(now_in_second.ToString() + "," + pid.SetPoint.ToString() + "," + current_value + "," + output.ToString() + "\r\n");
            sw_csv.Close();

            StreamReader sr = new StreamReader(filepath, Encoding.GetEncoding("Shift_JIS"));
            string s = sr.ReadToEnd();
            sr.Close();
            s = s.Replace("\"LAST_VALUE\": \"" + jobj["LAST_VALUE"].ToString() + "\",\r\n", "\"LAST_VALUE\": \"" + current_value + "\",\r\n");
            s = s.Replace("\"INTEGRAL_TERM\": \"" + jobj["INTEGRAL_TERM"].ToString() + "\",\r\n", "\"INTEGRAL_TERM\": \"" + pid.IntegralTerm + "\",\r\n");

            if (!feedback_mode)
            {
                if (mode_of_feedforward == 1 && output >= pid.OutputMax) mode_of_feedforward = 2;
                else if (mode_of_feedforward == 2 && output <= pid.OutputMin) mode_of_feedforward = 3;
                s = s.Replace("\"FEEDFORWARDMODE\": \"" + jobj["FEEDFORWARDMODE"].ToString() + "\",\r\n", "\"FEEDFORWARDMODE\": \"" + mode_of_feedforward + "\",\r\n");
            }

            StreamWriter sw = new StreamWriter(filepath, false, Encoding.GetEncoding("Shift_JIS"));
            sw.Write(s);
            sw.Close();

            Console.WriteLine(output);
        }
    }
    public class PID
    {
        public double? current { get; set; }
        public double? output { get; set; }
    }
    public sealed class PIDMap : CsvHelper.Configuration.ClassMap<PID>
    {
        public PIDMap()
        {
            Map(m => m.current).Index(0);
            Map(m => m.output).Index(1);
        }
    }
}
