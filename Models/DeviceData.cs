namespace DotnetTaskV4.Models
{
    public class DeviceData
    {
        public const string CheckResultFail = "FAIL";
        public const string CheckResultPass = "PASS";
        public const string DataDelimiter = ", ";

        public string Band { get; set; }
        public int Plc { get; set; }
        public float TxPower { get; set; }
        public float TargetPower { get; set; }
        public float MinPower { get; set; }
        public float MaxPower { get; set; }
        public bool CheckResult { get; set; }
    }
}
