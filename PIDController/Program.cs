using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;

namespace PIDController
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = "setting.json";
            string filepath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + filename;
            var jobj = JObject.Parse(File.ReadAllText(filepath));
            
            //  FIXME : P, I, D は設定の必要がある．
            double GAIN_P = double.Parse(jobj["GAIN_P"].ToString());
            double GAIN_I = double.Parse(jobj["GAIN_I"].ToString());
            double GAIN_D = double.Parse(jobj["GAIN_D"].ToString());
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
            TimeSpan pastTimeFromLastUpdate = new TimeSpan(0, 0, 0, 1);
            double output = pid.ControlVariable(pastTimeFromLastUpdate);
            Console.WriteLine(output);
            //return output;
        }
    }
}
