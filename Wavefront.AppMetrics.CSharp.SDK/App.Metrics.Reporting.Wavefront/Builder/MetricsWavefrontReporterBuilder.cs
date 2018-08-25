using System;
using App.Metrics.Builder;
using Wavefront.CSharp.SDK.Common;

namespace App.Metrics.Reporting.Wavefront.Builder
{
    /// <summary>
    ///     Builder for configuring metrics reporting to Wavefront using an
    ///     <see cref="IMetricsReportingBuilder" />.
    /// </summary>
    public static class MetricsWavefrontReporterBuilder
    {
        /// <summary>
        ///     Add the <see cref="WavefrontReporter" /> allowing metrics to be reported to Wavefront.
        /// </summary>
        /// <param name="metricReporterProviderBuilder">
        ///     The <see cref="IMetricsReportingBuilder" /> used to configure metrics reporters.
        /// </param>
        /// <param name="options">The Wavefront reporting options to use.</param>
        /// <returns>
        ///     An <see cref="IMetricsBuilder" /> that can be used to further configure App Metrics.
        /// </returns>
        public static IMetricsBuilder ToWavefront(
            this IMetricsReportingBuilder metricReporterProviderBuilder,
            MetricsReportingWavefrontOptions options)
        {
            if (metricReporterProviderBuilder == null)
            {
                throw new ArgumentNullException(nameof(metricReporterProviderBuilder));
            }

            var provider = new WavefrontReporter(options);

            return metricReporterProviderBuilder.Using(provider);
        }

        /// <summary>
        ///     Add the <see cref="WavefrontReporter" /> allowing metrics to be reported to Wavefront.
        /// </summary>
        /// <param name="metricReporterProviderBuilder">
        ///     The <see cref="IMetricsReportingBuilder" /> used to configure metrics reporters.
        /// </param>
        /// <param name="setupAction">The Wavefront reporting options to use.</param>
        /// <returns>
        ///     An <see cref="IMetricsBuilder" /> that can be used to further configure App Metrics.
        /// </returns>
        public static IMetricsBuilder ToWavefront(
            this IMetricsReportingBuilder metricReporterProviderBuilder,
            Action<MetricsReportingWavefrontOptions> setupAction)
        {
            if (metricReporterProviderBuilder == null)
            {
                throw new ArgumentNullException(nameof(metricReporterProviderBuilder));
            }

            var options = new MetricsReportingWavefrontOptions();

            setupAction?.Invoke(options);

            var provider = new WavefrontReporter(options);

            return metricReporterProviderBuilder.Using(provider);
        }

        /// <summary>
        ///     Add the <see cref="WavefrontReporter" /> allowing metrics to be reported to Wavefront.
        /// </summary>
        /// <param name="metricReporterProviderBuilder">
        ///     The <see cref="IMetricsReportingBuilder" /> used to configure metrics reporters.
        /// </param>
        /// <param name="wavefrontSender">
        ///     The <see cref="IWavefrontSender" /> that handles the formatting and flushing of metrics
        ///     to Wavefront, either via direct ingestion or the Wavefront Proxy Agent.
        /// </param>
        /// <returns>
        ///     An <see cref="IMetricsBuilder" /> that can be used to further configure App Metrics.
        /// </returns>
        public static IMetricsBuilder ToWavefront(
            this IMetricsReportingBuilder metricReporterProviderBuilder,
            IWavefrontSender wavefrontSender)
        {
            if (metricReporterProviderBuilder == null)
            {
                throw new ArgumentNullException(nameof(metricReporterProviderBuilder));
            }

            var options = new MetricsReportingWavefrontOptions
            {
                WavefrontSender = wavefrontSender
            };

            var provider = new WavefrontReporter(options);

            return metricReporterProviderBuilder.Using(provider);
        }

        /// <summary>
        ///     Add the <see cref="WavefrontReporter" /> allowing metrics to be reported to Wavefront.
        /// </summary>
        /// <param name="metricReporterProviderBuilder">
        ///     The <see cref="IMetricsReportingBuilder" /> used to configure metrics reporters.
        /// </param>
        /// <param name="wavefrontSender">
        ///     The <see cref="IWavefrontSender" /> that handles the formatting and flushing of metrics
        ///     to Wavefront, either via direct ingestion or the Wavefront Proxy Agent.
        /// </param>
        /// <param name="source">
        ///     The source of your metrics.
        /// </param>
        /// <returns>
        ///     An <see cref="IMetricsBuilder" /> that can be used to further configure App Metrics.
        /// </returns>
        public static IMetricsBuilder ToWavefront(
            this IMetricsReportingBuilder metricReporterProviderBuilder,
            IWavefrontSender wavefrontSender,
            string source)
        {
            if (metricReporterProviderBuilder == null)
            {
                throw new ArgumentNullException(nameof(metricReporterProviderBuilder));
            }

            var options = new MetricsReportingWavefrontOptions
            {
                WavefrontSender = wavefrontSender,
                Source = source
            };

            var provider = new WavefrontReporter(options);

            return metricReporterProviderBuilder.Using(provider);
        }
    }
}
