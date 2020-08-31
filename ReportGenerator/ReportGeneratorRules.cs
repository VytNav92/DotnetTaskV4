using DotnetTaskV4.Models;
using System;
using System.Collections.Generic;

namespace DotnetTaskV4.ReportGenerator
{
    public static class ReportGeneratorRules
    {
        public static IReadOnlyCollection<string> HeaderColumnNames = new string[]
            {
                "BAND",
                "PCL",
                "Low TxPower(avg)",
                "In Range TxPower(avg)",
                "High TxPower(avg)",
                "PASS Count",
                "FAIL Count"
            };

        public static IReadOnlyCollection<Func<ReportData, string>> DataRowGenerationRules = new Func<ReportData, string>[]
            {
                data => data.Band.ToString(),
                data => data.Plc.ToString(),
                data => (data.LowTxPowerSum / (data.PassCount + data.FailCount)).ToString(),
                data => (data.InRangeTxPowerSum / (data.PassCount + data.FailCount)).ToString(),
                data => (data.HighTxPowerSum / (data.PassCount + data.FailCount)).ToString(),
                data => data.PassCount.ToString(),
                data => data.FailCount.ToString(),
            };
    }
}
