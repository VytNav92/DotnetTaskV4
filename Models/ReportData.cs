namespace DotnetTaskV4.Models
{
    public class ReportData
    {
        public string Band { get; set; }
        public int Plc { get; set; }
        public float InRangeTxPowerSum { get; set; }
        public float LowTxPowerSum { get; set; }
        public float HighTxPowerSum { get; set; }
        public uint PassCount { get; set; }
        public uint FailCount { get; set; }
    }
}
