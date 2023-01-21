using System.Threading.Tasks;
using JasperFx.Core;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace PersistenceTests.Samples;

public static class DurabilityAgentCustomization
{
    public static async Task AdvancedConfigurationOfDurabilityAgent()
    {
        #region sample_AdvancedConfigurationOfDurabilityAgent

        using var host = await Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                // Control the maximum batch size of recovered
                // messages that the current node will try
                // to pull into itself
                opts.Node.RecoveryBatchSize = 500;


                // How soon should the first node reassignment
                // execution to try to look for dormant nodes
                // run?
                opts.Node.FirstNodeReassignmentExecution = 1.Seconds();

                // Fine tune how the polling for ready to execute
                // or send scheduled messages
                opts.Node.ScheduledJobFirstExecution = 0.Seconds();
                opts.Node.ScheduledJobPollingTime = 60.Seconds();
            }).StartAsync();

        #endregion
    }
}