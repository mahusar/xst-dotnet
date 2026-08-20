using Xunit;

namespace Xst.Rpc.IntegrationTests
{
    public sealed class DaemonFactAttribute : FactAttribute
    {
        public DaemonFactAttribute()
        {
            if (!DaemonSettings.IsConfigured)
            {
                Skip = "No daemon configured. Set " + DaemonSettings.UserVariable +
                       " and " + DaemonSettings.PasswordVariable + " to run this.";
            }
        }
    }

    public sealed class ExploreFactAttribute : FactAttribute
    {
        public ExploreFactAttribute()
        {
            if (!DaemonSettings.IsConfigured)
            {
                Skip = "No daemon configured. Set " + DaemonSettings.UserVariable +
                       " and " + DaemonSettings.PasswordVariable + " to run this.";
            }
            else if (!DaemonSettings.HasExploreApi)
            {
                Skip = "Daemon is not running the explore API. Set " +
                       DaemonSettings.ExploreVariable + "=1 once it is.";
            }
        }
    }

    public sealed class SpendFactAttribute : FactAttribute
    {
        public SpendFactAttribute()
        {
            if (!DaemonSettings.IsConfigured)
            {
                Skip = "No daemon configured. Set " + DaemonSettings.UserVariable +
                       " and " + DaemonSettings.PasswordVariable + " to run this.";
            }
            else if (!DaemonSettings.AllowsSpending)
            {
                Skip = "This test moves coins. Set " + DaemonSettings.SpendVariable +
                       "=1 to run it, plus " + DaemonSettings.MainnetSpendVariable +
                       "=1 if the daemon is on mainnet.";
            }
        }
    }
}
