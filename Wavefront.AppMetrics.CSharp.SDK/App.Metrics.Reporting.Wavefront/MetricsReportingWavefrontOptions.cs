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
            WavefrontHistogram = new MetricsReportingWavefrontHistogramOptions();
        }

        /// <summary>
        ///     Gets or sets the Wavefront sender that handles the formatting and flushing of
        ///     metrics to Wavefront, either via direct ingestion or the Wavefront Proxy Agent.
        /// </summary>
        /// <value>The Wavefront direct injestion or proxy client</value>
        public IWavefrontSender WavefrontSender { get; set; }

        /// <summary>
        ///     Gets or sets the source of your metrics.
        /// </summary>
        /// <value>The source.</value>
        public string Source { get; set; }

        /// <summary>
        ///     Gets the options that pertain to the reporting of Wavefront Histograms.
        /// </summary>
        /// <value>The options for the reporting of Wavefront Histograms.</value>
        public MetricsReportingWavefrontHistogramOptions WavefrontHistogram { get; }

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
