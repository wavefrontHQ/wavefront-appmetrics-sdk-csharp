using System;
using App.Metrics.Filters;
using Wavefront.CSharp.SDK.Common;

namespace App.Metrics.Reporting.Wavefront
{
    /// <summary>
    ///     Provides programmatic configuration of Wavefront reporting in the App Metrics framework.
    /// </summary>
    public class MetricsReportingWavefrontOptions
    {
        public MetricsReportingWavefrontOptions()
        {
            Source = "app-metrics";
        }

        /// <summary>
        ///     Gets or sets the Wavefront sender that handles the formatting and flushing of
        ///     metrics to Wavefront, either via direct ingestion or the Wavefront Proxy Agent.
        /// </summary>
        public IWavefrontSender WavefrontSender { get; set; }

        /// <summary>
        ///     Gets or sets the source of your metrics.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        ///     Gets or sets the <see cref="IFilterMetrics" /> to use for just this reporter.
        /// </summary>
        /// <value>
        ///     The <see cref="IFilterMetrics" /> to use for this reporter.
        /// </value>
        public IFilterMetrics Filter { get; set; }

        /// <summary>
        ///     Gets or sets the interval between flushing metrics.
        /// </summary>
        public TimeSpan FlushInterval { get; set; }
    }
}
