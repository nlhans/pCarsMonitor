using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pCarsTelemetry.API;

namespace pCarsMonitor
{
    public partial class Debug : Form
    {
        public Debug(DataController data)
        {
            InitializeComponent();

            data.Data += data_Data;
        }

        private void data_Data(PcarsTelemetrySample data, PcarsExtraData myData)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new TelemetryDelegate(data_Data), new object[2] {data, myData});
                return;
            }

            // Get all properties
            var fields = typeof (PcarsTelemetrySample).GetFields();

            var lbl = "";
            var val = "";
            var lbl2 = "";
            var val2 = "";
            var lines = 0;

            foreach (var f in fields)
            {

                var v = f.GetValue(data);
                string vs = string.Empty;
                if (v is float[])
                {
                    vs += string.Join(",", (v as float[])) + "\r\n";
                }
                else if (v is int[])
                {
                    vs += string.Join(",", (v as int[])) + "\r\n";
                }
                else
                {
                    vs += v.ToString() + "\r\n";
                }

                if (lines > 50)
                    lbl2 += f.Name + "\r\n";
                else
                    lbl += f.Name + "\r\n";

                if (lines > 50)
                    val2 += vs;
                else
                    val += vs;

                lines++;
            }

            // Get me
            fields = typeof (PcarsParticipant).GetFields();
            var me = data.mParticipantData.FirstOrDefault();
            foreach (var f in fields)
            {

                var v = f.GetValue(me);
                string vs = string.Empty;
                if (v is float[])
                {
                    vs += string.Join(",", (v as float[])) + "\r\n";
                }
                else if (v is int[])
                {
                    vs += string.Join(",", (v as int[])) + "\r\n";
                }
                else
                {
                    vs += v.ToString() + "\r\n";
                }

                lbl += f.Name + "\r\n";
                val += vs;

                lines++;
            }


            label1.Text = lbl;
            label2.Text = val;
            label4.Text = lbl2;
            label3.Text = val2;
        }
    }
}