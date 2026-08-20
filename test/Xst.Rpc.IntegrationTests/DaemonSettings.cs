using System;
using System.Globalization;
using Xst.Rpc;

namespace Xst.Rpc.IntegrationTests
{
    internal static class DaemonSettings
    {
        internal const string HostVariable = "XST_RPC_HOST";
        internal const string PortVariable = "XST_RPC_PORT";
        internal const string UserVariable = "XST_RPC_USER";
        internal const string PasswordVariable = "XST_RPC_PASSWORD";
        internal const string ExploreVariable = "XST_RPC_EXPLORE";
        internal const string SpendVariable = "XST_RPC_ALLOW_SPEND";
        internal const string MainnetSpendVariable = "XST_RPC_ALLOW_MAINNET_SPEND";
        internal const string SpendAmountVariable = "XST_RPC_SPEND_AMOUNT";

        internal const decimal DefaultSpendAmount = 0.01m;

        internal const decimal MaxSpendAmount = 0.05m;

        internal static string Host => Read(HostVariable) ?? "127.0.0.1";
        internal static string Username => Read(UserVariable);
        internal static string Password => Read(PasswordVariable);

        internal static int Port
        {
            get
            {
                var raw = Read(PortVariable);
                return int.TryParse(raw, out var port) ? port : XstClientOptions.DefaultPort;
            }
        }

        internal static bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        internal static bool HasExploreApi => IsTruthy(Read(ExploreVariable));

        internal static bool AllowsSpending => IsTruthy(Read(SpendVariable));

        internal static bool AllowsMainnetSpending => IsTruthy(Read(MainnetSpendVariable));

        internal static decimal SpendAmount
        {
            get
            {
                var raw = Read(SpendAmountVariable);
                if (string.IsNullOrWhiteSpace(raw)) return DefaultSpendAmount;

                if (!decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture,
                                      out var amount))
                {
                    throw new FormatException(
                        SpendAmountVariable + " is not an invariant decimal amount: " + raw);
                }

                return amount;
            }
        }

        internal static decimal RequireSpendAmount()
        {
            var amount = SpendAmount;

            if (amount <= 0m)
            {
                throw new InvalidOperationException(
                    SpendAmountVariable + " must be positive, got " + XstAmount.Format(amount));
            }

            if (amount > MaxSpendAmount)
            {
                throw new InvalidOperationException(
                    "Refusing to move " + XstAmount.Format(amount) + " XST. The cap is " +
                    XstAmount.Format(MaxSpendAmount) + " -- raise MaxSpendAmount in code if " +
                    "that is really intended, not from the environment.");
            }

            if (amount != XstAmount.Round(amount))
            {
                throw new InvalidOperationException(
                    "Amount " + amount + " carries more than " + XstAmount.Decimals +
                    " decimals; the daemon would round it silently.");
            }

            return amount;
        }

        internal static XstClient CreateClient()
        {
            return new XstClient(new XstClientOptions
            {
                Host = Host,
                Port = Port,
                Username = Username,
                Password = Password,
                Timeout = TimeSpan.FromSeconds(30)
            });
        }

        private static string Read(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        private static bool IsTruthy(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            return value.Equals("1", StringComparison.Ordinal) ||
                   value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
