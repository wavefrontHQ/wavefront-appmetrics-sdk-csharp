using App.Metrics.Filters;
using App.Metrics.Formatters;
using App.Metrics.Formatters.Wavefront;
using App.Metrics.Internal;
using App.Metrics.Logging;
using App.Metrics.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Wavefront.SDK.CSharp.Common;
using Wavefront.SDK.CSharp.Common.Metrics;
using Wavefront.SDK.CSharp.Entities.Histograms;

namespace App.Metrics.Reporting.Wavefront
{
    /// <summary>
    ///     Implementation of App Metrics reporter that handles reporting to Wavefront.
    /// </summary>
    public class WavefrontReporter : IReportMetrics
    {
        private static readonly ILog Logger = LogProvider.For<WavefrontReporter>();

        private readonly IWavefrontSender wavefrontSender;
        private readonly string source;
        private readonly IDictionary<string, string> globalTags;
        private readonly ISet<HistogramGranularity> histogramGranularities;
        private readonly MetricFields metricFields;
        private readonly WavefrontSdkMetricsRegistry sdkMetricsRegistry;

        private readonly WavefrontSdkDeltaCounter reporterErrors;

        public WavefrontReporter(MetricsReportingWavefrontOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.WavefrontSender == null)
            {
                throw new ArgumentNullException(
                    nameof(MetricsReportingWavefrontOptions.WavefrontSender));
            }

            wavefrontSender = options.WavefrontSender;

            source = options.Source;

            if (options.ApplicationTags != null)
            {
                globalTags = new Dictionary<string, string>(options.ApplicationTags.ToPointTags());
            }
            else
            {
                globalTags = new Dictionary<string, string>();
            }

            histogramGranularities = new HashSet<HistogramGranularity>();
            if (options.WavefrontHistogram.ReportMinuteDistribution)
            {
                histogramGranularities.Add(HistogramGranularity.Minute);
            }
            if (options.WavefrontHistogram.ReportHourDistribution)
            {
                histogramGranularities.Add(HistogramGranularity.Hour);
            }
            if (options.WavefrontHistogram.ReportDayDistribution)
            {
                histogramGranularities.Add(HistogramGranularity.Day);
            }

            if (options.FlushInterval < TimeSpan.Zero)
            {
                throw new InvalidOperationException(
                    $"{nameof(MetricsReportingWavefrontOptions.FlushInterval)} " +
                    "must not be less than zero");
            }

            Filter = options.Filter;

            FlushInterval = options.FlushInterval > TimeSpan.Zero
                ? options.FlushInterval
                : AppMetricsConstants.Reporting.DefaultFlushInterval;

            // Formatting will be handled by the Wavefront sender.
            Formatter = null;

            metricFields = options.MetricFields ?? new MetricFields();

            var registryBuilder = new WavefrontSdkMetricsRegistry.Builder(wavefrontSender)
                .Prefix(Constants.SdkMetricPrefix + ".app_metrics")
                .Source(source)
                .Tags(globalTags);
            if (options.LoggerFactory != null)
            {
                registryBuilder.LoggerFactory(options.LoggerFactory);
            }
            sdkMetricsRegistry = registryBuilder.Build();

            reporterErrors = sdkMetricsRegistry.DeltaCounter("reporter.errors");

            double sdkVersion = Utils.GetSemVer(Assembly.GetExecutingAssembly());
            sdkMetricsRegistry.Gauge("version", () => sdkVersion);

            Logger.Info($"Using Wavefront Reporter {this}. FlushInterval: {FlushInterval}");
        }

        /// <inheritdoc />
        public IFilterMetrics Filter { get; set; }

        /// <inheritdoc />
        public TimeSpan FlushInterval { get; set; }

        /// <inheritdoc />
        public IMetricsOutputFormatter Formatter { get; set; }

        /// <summary>
        ///     Flushes the current metrics snapshot in Wavefront data format.
        /// </summary>
        /// <param name="metricsData">The current snapshot of metrics.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if metrics were successfully flushed, false otherwise.</returns>
        public async Task<bool> FlushAsync(
            MetricsDataValueSource metricsData,
            CancellationToken cancellationToken = default)
        {
            Logger.Trace("Flushing metrics snapshot");

            try
            {
                await WriteAsync(metricsData, cancellationToken);
            }
            catch (Exception e)
            {
                reporterErrors.Inc();
                Logger.Error(e.Message);
                return false;
            }

            Logger.Trace("Flushed metrics snapshot");
            return true;
        }

        /// <summary>
        ///     Writes the specified <see cref="MetricsDataValueSource" /> to the configured
        ///     <see cref="IWavefrontSender" />.
        /// </summary>
        /// <param name="metricsData">
        ///     The <see cref="MetricsDataValueSource" /> being written.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken" /></param>
        /// <returns>A <see cref="Task" /> representing the asynchronous write operation.</returns>

#if NET452
        private Task WriteAsync(
            MetricsDataValueSource metricsData,
            CancellationToken cancellationToken = default)
        {
            var serializer = new MetricSnapshotSerializer();

            using (var writer = new MetricSnapshotWavefrontWriter(wavefrontSender, source,
                globalTags, histogramGranularities, sdkMetricsRegistry, metricFields))
            {
                serializer.Serialize(writer, metricsData, metricFields);
            }

            return AppMetricsTaskHelper.CompletedTask();
        }
#else
        private async Task WriteAsync(
            MetricsDataValueSource metricsData,
            CancellationToken cancellationToken = default)
        {
            var serializer = new MetricSnapshotSerializer();

            await using var writer = new MetricSnapshotWavefrontWriter(wavefrontSender, source,
                globalTags, histogramGranularities, sdkMetricsRegistry, metricFields);
            serializer.Serialize(writer, metricsData, metricFields);
        }
#endif
    }
}
