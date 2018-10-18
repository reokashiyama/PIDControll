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
            int half_timeSpan = int.Parse(jobj["HALF_TIMESPAN"].ToString());
            int test_duration = int.Parse(jobj["TEST_DURATION"].ToString());
            int const_value = int.Parse(jobj["CONST_VALUE"].ToString());
            double GAIN_P = double.Parse(jobj["GAIN_P"].ToString());
            double GAIN_I = double.Parse(jobj["GAIN_I"].ToString());
            double GAIN_D = double.Parse(jobj["GAIN_D"].ToString());
            double OUTPUT_MAX = double.Parse(jobj["OUTPUT_MAX"].ToString());
            double OUTPUT_MIN = double.Parse(jobj["OUTPUT_MIN"].ToString());
            double output_last = double.Parse(jobj["OUTPUT_LAST"].ToString());
            double MAX_POWER = double.Parse(jobj["MAX_POWER"].ToString());
            double RESISTANCE_VALUE = double.Parse(jobj["RESISTANCE_VALUE"].ToString());
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
            test_duration += SamplingSpan;

            if (feedback_mode)
            {
                output = pid.ControlVariable(jobj, pastTimeFromLastUpdate);
            }
            else
            {
                if (test_duration <= half_timeSpan)
                {
                    output = Math.Sqrt(MAX_POWER / half_timeSpan * RESISTANCE_VALUE * test_duration);
                }
                else if (half_timeSpan < test_duration && test_duration <= 2 * half_timeSpan)
                {
                    output = Math.Sqrt(2 * MAX_POWER * RESISTANCE_VALUE - MAX_POWER / half_timeSpan * RESISTANCE_VALUE * test_duration);
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
            s = s.Replace("\"OUTPUT_LAST\": \"" + jobj["OUTPUT_LAST"].ToString() + "\",\r\n", "\"OUTPUT_LAST\": \"" + output + "\",\r\n");
            s = s.Replace("\"TEST_DURATION\": \"" + jobj["TEST_DURATION"].ToString() + "\",\r\n", "\"TEST_DURATION\": \"" + test_duration + "\",\r\n");

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
