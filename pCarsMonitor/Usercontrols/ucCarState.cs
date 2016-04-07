using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pCarsTelemetry.API;

namespace pCarsMonitor.Usercontrols
{
    public partial class ucCarState : UserControl
    {
        private Timer updateUi;
        private DataController data;
        private PcarsTelemetrySample telemSample;

        public ucCarState(DataController datactr)
        {
            data = datactr;
            data.Data += data_Data;

            InitializeComponent();

            updateUi = new Timer {Interval = 50};
            updateUi.Tick += updateUi_Tick;
            updateUi.Start();

            label1.ForeColor = Color.White;
            label2.ForeColor = Color.White;
        }

        void data_Data(PcarsTelemetrySample sample, PcarsExtraData myData)
        {
            telemSample = sample;
        }

        private Dictionary<uint, float> fuel = new Dictionary<uint, float>(); 

        void updateUi_Tick(object sender, EventArgs e)
        {
            if (telemSample.mTyreSlipSpeed == null)
                return;
            if (telemSample.mRPM >= 6000)
                BackColor = Color.Firebrick;
            else if (telemSample.mTyreSlipSpeed[0] > 5 || telemSample.mTyreSlipSpeed[1] > 5)
                BackColor = Color.Blue;
            else
                BackColor = Color.Black;

            var me = telemSample.mParticipantData.FirstOrDefault();
            if (fuel.ContainsKey(me.mCurrentLap))
                fuel[me.mCurrentLap] = Math.Min(fuel[me.mCurrentLap], telemSample.mFuelLevel * telemSample.mFuelCapacity);
            else
                fuel.Add(me.mCurrentLap, telemSample.mFuelLevel * telemSample.mFuelCapacity);

            // Consumption
            var fuelS = "";

            var lastFuel = 0.0;
            foreach (var kvp in fuel)
            {
                var consumption = lastFuel - kvp.Value;
                if (lastFuel > 0)
                {
                    fuelS += Math.Round(lastFuel,2) + " (-" + Math.Round(consumption,2) + "), ";
                }
                lastFuel = kvp.Value;
            }

            label1.Text = string.Format("Damage. Engine: {0:000.00}% Aero: {1:000.00}% Ambient: {2:00.0}C Track: {3:00.0C} Rain: {4}", Math.Round(telemSample.mEngineDamage*100, 3), Math.Round(telemSample.mAeroDamage*100, 2), telemSample.mAmbientTemperature, telemSample.mTrackTemperature, telemSample.mRainDensity);
            label2.Text = string.Format(
                "Wear FL {0:000.000} FR {1:000.000} LR: {2:000.000} RR {3:000.000}\r\nFuel: {4}\r\nSuspension: {5}",
                telemSample.mTyreWear[0]*100,
                telemSample.mTyreWear[1]*100,
                telemSample.mTyreWear[2]*100,
                telemSample.mTyreWear[3]*100, fuelS,
                string.Join(",", telemSample.mSuspensionDamage.Select(x => (x*100).ToString("000.00"))));
        }
    }
}
