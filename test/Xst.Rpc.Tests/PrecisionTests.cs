using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xst.Rpc;
using Xunit;

namespace Xst.Rpc.Tests
{
    public class PrecisionTests
    {
        [Fact]
        public void Parser_reads_fractional_numbers_as_decimal_not_double()
        {
            var parsed = Xst.Rpc.Internal.JsonRpcTransport.ParseJson("{\"result\":1.1}");
            Assert.Equal(JTokenType.Float, parsed["result"].Type);
            Assert.IsType<decimal>(((JValue)parsed["result"]).Value);
        }

        [Fact]
        public async Task Balance_survives_full_six_decimal_precision()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("12345678.123456");

            var balance = await client.GetBalanceAsync();

            Assert.Equal(12345678.123456m, balance);
        }

        [Fact]
        public async Task Balance_near_max_supply_is_exact()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("43299999.999999");

            var balance = await client.GetBalanceAsync();

            Assert.Equal(43299999.999999m, balance);
        }

        [Fact]
        public void A_float_would_have_lost_this_and_a_decimal_does_not()
        {
            const decimal exact = 12345678.123456m;

            var viaFloat = (decimal)(float)exact;
            var viaDouble = (decimal)(double)exact;

            Assert.NotEqual(exact, viaFloat);
            Assert.Equal(exact, viaDouble);
        }

        [Theory]
        [InlineData("0.000001", 0.000001)]
        [InlineData("0.1", 0.1)]
        [InlineData("100.5", 100.5)]
        [InlineData("0", 0)]
        public async Task Small_amounts_round_trip(string json, decimal expected)
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(json);

            Assert.Equal(expected, await client.GetBalanceAsync());
        }
    }

    public class XstAmountTests
    {
        [Fact]
        public void Xst_has_six_decimals_not_eight()
        {
            Assert.Equal(6, XstAmount.Decimals);
            Assert.Equal(1000000L, XstAmount.UnitsPerXst);
        }

        [Theory]
        [InlineData(1.0, 1000000L)]
        [InlineData(0.000001, 1L)]
        [InlineData(123.456789, 123456789L)]
        public void Converts_to_units(decimal xst, long expected)
        {
            Assert.Equal(expected, XstAmount.ToUnits(xst));
        }

        [Fact]
        public void Rounds_beyond_six_decimals_rather_than_letting_the_daemon_do_it()
        {
            Assert.Equal(1.234568m, XstAmount.Round(1.2345678m));
            Assert.Equal(1.234567m, XstAmount.Round(1.2345674m));
        }

        [Fact]
        public void Formats_with_invariant_culture()
        {
            Assert.Equal("1.5", XstAmount.Format(1.5m));
            Assert.Equal("0.000001", XstAmount.Format(0.000001m));
            Assert.Equal("100", XstAmount.Format(100m));
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        [InlineData(1, true)]
        [InlineData(43300000, true)]
        [InlineData(43300001, false)]
        public void Guards_the_sendable_range(decimal amount, bool expected)
        {
            Assert.Equal(expected, XstAmount.IsSendable(amount));
        }
    }
}
