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
            double GAIN_P = double.Parse(jobj["GAIN_P"].ToString());
            double GAIN_I = double.Parse(jobj["GAIN_I"].ToString());
            double GAIN_D = double.Parse(jobj["GAIN_D"].ToString());
            double OUTPUT_MAX = double.Parse(jobj["OUTPUT_MAX"].ToString());
            double OUTPUT_MIN = double.Parse(jobj["OUTPUT_MIN"].ToString());

            PidController pid = new PidController(GAIN_P, GAIN_I, GAIN_D, OUTPUT_MAX, OUTPUT_MIN);

            SerialPort LKG5000 = new SerialPort(jobj["PORT_NAME"].ToString(), int.Parse(jobj["BAURATE"].ToString()), Parity.None, 8, StopBits.One);
            LKG5000.Open();
            LKG5000.Write("MS,01\r");
            string returnstring = LKG5000.ReadLine();
            double current_value = double.Parse(returnstring.Substring(6, 8));
            LKG5000.Close();

            pid.SetPoint = double.Parse(jobj["SET_POINT"].ToString());
            pid.ProcessVariable = current_value;
            pid.ProcessVariableLast = double.Parse(jobj["LAST_VALUE"].ToString());
            //  TODO    : change LAST_VALUE 

            string logfilepath = Directory.GetCurrentDirectory() + /*c + ".." + c + "Log" +*/ c + jobj["LOG_FILE_NAME"].ToString();

            TimeSpan pastTimeFromLastUpdate = new TimeSpan(0, 0, 0, SamplingSpan);
            double output = pid.ControlVariable(jobj, pastTimeFromLastUpdate, logfilepath);

            System.IO.StreamWriter sw_csv = new System.IO.StreamWriter(logfilepath,true,System.Text.Encoding.GetEncoding("shift_jis"));
            sw_csv.Write(pid.SetPoint.ToString() + "," + current_value + "," + output.ToString() + "\r\n");
            sw_csv.Close();

            StreamReader sr = new StreamReader(filepath, Encoding.GetEncoding("Shift_JIS"));
            string s = sr.ReadToEnd();
            sr.Close();
            s = s.Replace("\"LAST_VALUE\": " + double.Parse(jobj["LAST_VALUE"].ToString()) + ",", "\"LAST_VALUE\": " + current_value + ",");
            s = s.Replace("\"INTEGRAL_TERM\": " + double.Parse(jobj["INTEGRAL_TERM"].ToString()) + ",", "\"LAST_VALUE\": " + pid.IntegralTerm + ",");

            StreamWriter sw = new StreamWriter(filepath, false, Encoding.GetEncoding("Shift_JIS"));
            sw.Write(s);
            sw.Close();

            Console.WriteLine(output);
            //return output;
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
