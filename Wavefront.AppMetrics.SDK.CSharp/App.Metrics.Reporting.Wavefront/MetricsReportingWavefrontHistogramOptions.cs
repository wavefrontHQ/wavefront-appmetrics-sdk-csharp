namespace App.Metrics.Reporting.Wavefront
{
    /// <summary>
    ///     Provides programmatic configuration of the reporting of Wavefront Histograms in the
    ///     App Metrics framework.
    /// </summary>
    public class MetricsReportingWavefrontHistogramOptions
    {
        /// <summary>
        ///     Gets or sets a value indicating whether to report Wavefront Histograms
        ///     aggregated into minute intervals.
        /// </summary>
        /// <value><c>true</c> to report in minute intervals, <c>false</c> otherwise.</value>
        public bool ReportMinuteDistribution { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to report Wavefront Histograms
        ///     aggregated into hour intervals.
        /// </summary>
        /// <value><c>true</c> to report in hour intervals, <c>false</c> otherwise.</value>
        public bool ReportHourDistribution { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to report Wavefront Histograms
        ///     aggregated into day intervals.
        /// </summary>
        /// <value><c>true</c> to report in day intervals, <c>false</c> otherwise.</value>
        public bool ReportDayDistribution { get; set; }
    }
}
