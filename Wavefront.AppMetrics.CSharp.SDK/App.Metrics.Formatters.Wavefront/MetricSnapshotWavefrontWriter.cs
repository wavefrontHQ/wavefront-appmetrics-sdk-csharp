using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.Metrics;
using App.Metrics.Serialization;
using Wavefront.CSharp.SDK.Common;
using static App.Metrics.AppMetricsConstants;

namespace App.Metrics.Formatters.Wavefront
{
    /// <summary>
    /// Handles the writing of a metrics snapshot to a Wavefront sender.
    /// </summary>
    public class MetricSnapshotWavefrontWriter : IMetricSnapshotWriter
    {
        private static readonly Regex SimpleNames = new Regex("[^a-zA-Z0-9_.\\-~]");

        private readonly IWavefrontSender wavefrontSender;
        private readonly string source;

        public MetricSnapshotWavefrontWriter(IWavefrontSender wavefrontSender, string source)
        {
            this.wavefrontSender = wavefrontSender;
            this.source = source;
            MetricNameMapping = new GeneratedMetricNameMapping();
        }

        /// <inheritdoc />
        public GeneratedMetricNameMapping MetricNameMapping { get; } = new GeneratedMetricNameMapping();

        public void Write(string context, string name, object value, MetricTags tags, DateTime timestamp)
        {
            Write(context, name, new[] { "value" }, new[] { value }, tags, timestamp);
        }

        /// <inheritdoc />
        public void Write(string context, string name, IEnumerable<string> columns, IEnumerable<object> values, MetricTags tags, DateTime timestamp)
        {
            // Do not report App Metrics' internal metrics (e.g., report_success counter) to Wavefront
            if (context == InternalMetricsContext)
            {
                return;
            }

            string metricTypeValue = tags.Values[Array.IndexOf(tags.Keys, Pack.MetricTagsTypeKey)];
            var fields = columns.Zip(values, (column, data) => new { column, data })
                                .ToDictionary(pair => pair.column, pair => pair.data);

            // Unable to use a switch statement because the condition values are readonly
            if (metricTypeValue == Pack.ApdexMetricTypeValue)
            {
                WriteApdex(context, name, fields, tags, timestamp);
            }
            else if (metricTypeValue == Pack.CounterMetricTypeValue)
            {
                WriteCounter(context, name, fields, tags, timestamp);
            }
            else if (metricTypeValue == Pack.GaugeMetricTypeValue)
            {
                WriteGauge(context, name, fields, tags, timestamp);
            }
            else if (metricTypeValue == Pack.HistogramMetricTypeValue)
            {
                WriteHistogram(context, name, fields, tags, timestamp);
            }
            else if (metricTypeValue == Pack.MeterMetricTypeValue)
            {
                WriteMeter(context, name, fields, tags, timestamp);
            }
            else if (metricTypeValue == Pack.TimerMetricTypeValue)
            {
                WriteMeter(context, name, fields, tags, timestamp);
                WriteHistogram(context, name, fields, tags, timestamp);
            }
        }

        private void WriteApdex(string context, string name, IDictionary<string, object> fields, MetricTags tags, DateTime timestamp)
        {
            foreach (var entry in MetricNameMapping.Apdex)
            {
                if (fields.ContainsKey(entry.Value))
                {
                    Write(context, name, entry.Value, fields[entry.Value], tags, timestamp);
                }
            }
        }

        private void WriteCounter(string context, string name, IDictionary<string, object> fields, MetricTags tags, DateTime timestamp)
        {
            foreach (var entry in MetricNameMapping.Counter)
            {
                if (fields.ContainsKey(entry.Value))
                {
                    Write(context, name, entry.Value, fields[entry.Value], tags, timestamp);
                }
            }
        }

        private void WriteGauge(string context, string name, IDictionary<string, object> fields, MetricTags tags, DateTime timestamp)
        {
            foreach (var entry in MetricNameMapping.Gauge)
            {
                if (fields.ContainsKey(entry.Value))
                {
                    Write(context, name, entry.Value, fields[entry.Value], tags, timestamp);
                }
            }
        }

        private void WriteHistogram(string context, string name, IDictionary<string, object> fields, MetricTags tags, DateTime timestamp)
        {
            foreach (var entry in MetricNameMapping.Histogram)
            {
                if (fields.ContainsKey(entry.Value))
                {
                    Write(context, name, entry.Value, fields[entry.Value], tags, timestamp);
                }
            }
        }

        private void WriteMeter(string context, string name, IDictionary<string, object> fields, MetricTags tags, DateTime timestamp)
        {
            foreach (var entry in MetricNameMapping.Meter)
            {
                if (fields.ContainsKey(entry.Value))
                {
                    Write(context, name, entry.Value, fields[entry.Value], tags, timestamp);
                }
            }
        }

        private void Write(string context, string name, string subname, object value, MetricTags tags, DateTime timestamp)
        {
            wavefrontSender.SendMetric(ConcatAndSanitize(context, name, subname), Convert.ToDouble(value),
                                       UnixTime(timestamp), source, tags.ToDictionary());
        }

        private string ConcatAndSanitize(params string[] components)
        {
            return Sanitize(String.Join(".", components));
        }

        private string Sanitize(string name)
        {
            return SimpleNames.Replace(name, "_");
        }

        private long UnixTime(DateTime timestamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((timestamp - epoch).TotalSeconds);
        }

        public void Dispose()
        {
        }
    }
}
