namespace App.Metrics
{
    /// <summary>
    ///     Class containing Wavefront-specific constants and related static methods.
    /// </summary>
    public static class WavefrontConstants
    {
        /// <summary>
        ///     The <see cref="MetricTags"/> key that is used to identify Wavefront-specific
        ///     metric types.
        /// </summary>
        public static readonly string WavefrontMetricTypeTagKey = "wavefrontMetricType";

        /// <summary>
        ///     Determines whether or not a set of <see cref="MetricTags"/> contains a particular
        ///     tag that identifies a Wavefront metric type.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the tags do identify the particular Wavefront metric type,
        ///     <c>false</c> otherwise.</returns>
        /// <param name="tags">The tags.</param>
        /// <param name="wavefrontMetricTypeTagValue">The Wavefront metric type tag value.</param>
        internal static bool IsWavefrontMetricType(
            MetricTags tags, string wavefrontMetricTypeTagValue)
        {
            for (int i = 0; i < tags.Count; ++i)
            {
                if (tags.Keys[i] == WavefrontMetricTypeTagKey &&
                    tags.Values[i] == wavefrontMetricTypeTagValue)
                {
                    return true;
                }

            }
            return false;
        }
    }
}
