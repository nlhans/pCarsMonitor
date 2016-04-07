using System.ComponentModel;

namespace pCarsAPI_Demo.Enumerations
{
    public enum TrackSector
    {
        [Description("Invalid Sector")]
        SECTOR_INVALID = 0,
        [Description("Sector Start")]
        SECTOR_START,
        [Description("Sector 1")]
        SECTOR_SECTOR1,
        [Description("Sector 2")]
        SECTOR_SECTOR2,
        [Description("Sector 3")]
        SECTOR_FINISH,
        [Description("Sector Stop??")]
        SECTOR_STOP,
        //-------------
        SECTOR_MAX
    }
}