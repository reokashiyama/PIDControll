using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using CsvHelper.Configuration;

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
            //  FIXME : P, I, D は設定の必要がある．
            int SamplingSpan = int.Parse(jobj["SamplingSpan"].ToString());
            double GAIN_P = double.Parse(jobj["GAIN_P"].ToString());
            double GAIN_I = double.Parse(jobj["GAIN_I"].ToString());
            double GAIN_D = double.Parse(jobj["GAIN_D"].ToString());
            string logfilepath = Directory.GetCurrentDirectory() + /*c + ".." + c + "Log" +*/ c + jobj["LogFileName"].ToString();
            //  FIXME : OUTPUTの上限値，下限値も決めておく必要がある．
            double OUTPUT_MAX = double.Parse(jobj["OUTPUT_MAX"].ToString());
            double OUTPUT_MIN = double.Parse(jobj["OUTPUT_MIN"].ToString());
            PidController pid = new PidController(GAIN_P, GAIN_I, GAIN_D, OUTPUT_MAX, OUTPUT_MIN);
            //  プログラム実行の第1引数は参照軌道値（TARGET）
            pid.SetPoint = double.Parse(args[0]);
            //  プログラム実行の第2引数は現在値（TARGET）
            pid.ProcessVariable = double.Parse(args[1]);
            //  プログラム実行の第3引数は1個前の値（TARGET）
            pid.ProcessVariableLast = double.Parse(args[2]);

            //  XXX : updateのspanは1秒ごとにしているが，要検討
            TimeSpan pastTimeFromLastUpdate = new TimeSpan(0, 0, 0, SamplingSpan);
            double output = pid.ControlVariable(pastTimeFromLastUpdate, logfilepath);

            System.IO.StreamWriter sw = new System.IO.StreamWriter(logfilepath,true,System.Text.Encoding.GetEncoding("shift_jis"));
            //TextBox1.Textの内容を書き込む
            sw.Write(pid.SetPoint.ToString() + "," + pid.ProcessVariable.ToString() + "," + output.ToString() + "\r\n");
            //閉じる
            sw.Close();

            Console.WriteLine(output);
            //return output;
        }
    }
    public class PID
    {
        public double? target { get; set; }
        public double? current { get; set; }
        public double? output { get; set; }
    }
    public sealed class PIDMap : CsvHelper.Configuration.ClassMap<PID>
    {
        public PIDMap()
        {
            Map(m => m.target).Index(0);
            Map(m => m.current).Index(1);
            Map(m => m.output).Index(2);
        }
    }
}
