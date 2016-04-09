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
            BackColor = Color.Black;

            var me = telemSample.mParticipantData.FirstOrDefault();
            var fuelL = telemSample.mFuelLevel*telemSample.mFuelCapacity;
            if (fuel.ContainsKey(me.mCurrentLap))
                fuel[me.mCurrentLap] = Math.Min(fuel[me.mCurrentLap],fuelL);
            else
                fuel.Add(me.mCurrentLap, fuelL);

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

            label1.Text = string.Format("Damage: ENG: {0:000.00}% AERO: {1:000}% SUSP: {2} ", 
                Math.Round(telemSample.mEngineDamage*100, 3), 
                Math.Round(telemSample.mAeroDamage*100, 2), 
                string.Join(",", telemSample.mSuspensionDamage.Select(x => (x*100).ToString("000"))));
            label2.Text = string.Format(
                "Wear FL {0:000.00} FR {1:000.00} LR: {2:000.00} RR {3:000.00}\r\nFuel: {4}\r\nAmbient: {5:00.0}C Track: {6:00.0C} Rain: {7}",
                telemSample.mTyreWear[0]*100,
                telemSample.mTyreWear[1]*100,
                telemSample.mTyreWear[2]*100,
                telemSample.mTyreWear[3] * 100, fuelS,

                telemSample.mAmbientTemperature, telemSample.mTrackTemperature, telemSample.mRainDensity);
        }
    }
}
