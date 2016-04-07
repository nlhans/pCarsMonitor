using System.Runtime.InteropServices;
using pCarsTelemetry.Enumerations;

namespace pCarsTelemetry.API
{
    public struct PcarsParticipant
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool mIsActive;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)PcarsApiConstraints.StringLength)]
        public string mName;                                    // [ string ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)Vector.VEC_MAX)]
        public float[] mWorldPosition;                          // [ UNITS = World Space  X  Y  Z ]
        
        public float mCurrentLapDistance;                       // [ UNITS = Metres ]   [ RANGE = 0.0f->... ]    [ UNSET = 0.0f ]
        public uint mRacePosition;                              // [ RANGE = 1->... ]   [ UNSET = 0 ]
        public uint mLapsCompleted;                             // [ RANGE = 0->... ]   [ UNSET = 0 ]
        public uint mCurrentLap;                                // [ RANGE = 0->... ]   [ UNSET = 0 ]
        public uint mCurrentSector;                             // [ enum (Type#4) Current Sector ]
    }
}