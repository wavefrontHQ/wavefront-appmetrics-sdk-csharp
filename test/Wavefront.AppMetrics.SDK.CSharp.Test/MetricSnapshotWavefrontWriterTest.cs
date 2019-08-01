using System.Collections.Generic;
using App.Metrics;
using App.Metrics.Formatters.Wavefront;
using Moq;
using Wavefront.SDK.CSharp.Common;
using Wavefront.SDK.CSharp.Common.Metrics;
using Wavefront.SDK.CSharp.Entities.Histograms;
using Xunit;

namespace Wavefront.AppMetrics.SDK.CSharp.Test
{
    public class MetricSnapshotWavefrontWriterTest
    {
        private readonly Mock<IWavefrontSender> wfSenderMock = new Mock<IWavefrontSender>();
        private readonly MetricSnapshotWavefrontWriter writer;

        public MetricSnapshotWavefrontWriterTest()
        {
            var wfSender = wfSenderMock.Object;
            var globalTags = new Dictionary<string, string>
            {
                { "globalKey1", "globalVal1" },
                { "globalKey2", "globalVal2" }
            };
            var histogramGranularities = new HashSet<HistogramGranularity>
            {
                HistogramGranularity.Minute
            };
            var sdkMetricsRegistry = new WavefrontSdkMetricsRegistry.Builder(wfSender).Build();
            writer = new MetricSnapshotWavefrontWriter(wfSender, "source", globalTags,
                histogramGranularities, sdkMetricsRegistry);
        }

        [Fact]
        public void TestFilterTags()
        {
            var metricTags = new MetricTags(
                new string[] { "globalKey1", "env", "location", "env" },
                new string[] { "pointValue1", "dev", "sf", "prod" });
            // Verify that global and point tags are included, and duplicate tag keys are handled.
            IDictionary<string, string> filteredTags = writer.FilterTags(metricTags);
            Assert.Equal(4, filteredTags.Count);
            Assert.Contains("globalKey1", filteredTags);
            Assert.Contains("globalKey2", filteredTags);
            Assert.Contains("env", filteredTags);
            Assert.Contains("location", filteredTags);
        }
    }
}
