using System;
using System.Globalization;

namespace Xst.Rpc
{
    public static class XstAmount
    {
        public const int Decimals = 6;

        public const long UnitsPerXst = 1000000L;

        public const decimal MaxMoney = 43300000m;

        public static decimal Round(decimal xst)
        {
            return Math.Round(xst, Decimals, MidpointRounding.AwayFromZero);
        }

        public static long ToUnits(decimal xst)
        {
            return (long)Round(xst * UnitsPerXst);
        }

        public static decimal FromUnits(long units)
        {
            return (decimal)units / UnitsPerXst;
        }

        public static string Format(decimal xst)
        {
            return Round(xst).ToString("0.######", CultureInfo.InvariantCulture);
        }

        public static decimal Parse(string text)
        {
            return decimal.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public static bool IsSendable(decimal xst)
        {
            return xst > 0m && xst <= MaxMoney;
        }
    }
}
