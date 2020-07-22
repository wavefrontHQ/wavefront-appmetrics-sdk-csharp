using App.Metrics.Filters;
using Microsoft.Extensions.Logging;
using System;
using Wavefront.SDK.CSharp.Common;
using Wavefront.SDK.CSharp.Common.Application;

namespace App.Metrics.Reporting.Wavefront
{
    /// <summary>
    ///     Provides programmatic configuration of Wavefront reporting in the App Metrics framework.
    /// </summary>
    public class MetricsReportingWavefrontOptions
    {
        public MetricsReportingWavefrontOptions()
        {
            Source = Utils.GetDefaultSource();
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

        /// <summary>
        ///     Gets or sets metadata about your application that is propagated as tags when
        ///     metrics/histograms are sent to Wavefront. This is an optional property.
        /// </summary>
        /// <value>The application tags.</value>
        public ApplicationTags ApplicationTags { get; set; }

        /// <summary>
        ///     Gets or sets the logger factory used to create internal loggers for Wavefront
        ///     reporting.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; set; }

        /// <summary>
        ///     Gets or sets the <see cref="Metrics.MetricFields" /> to use for just this reporter.
        /// </summary>
        public MetricFields MetricFields { get; set; }
    }
}
