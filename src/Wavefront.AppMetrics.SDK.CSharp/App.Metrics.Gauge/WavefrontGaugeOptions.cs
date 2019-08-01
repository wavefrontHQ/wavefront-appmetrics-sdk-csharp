namespace App.Metrics.Gauge
{
    /// <summary>
    ///     Implementation of GaugeOptions that overrides the default equality check so that
    ///     equality is based on a combination of Context, Name, and Tags.
    ///     This allows WavefrontGaugeOptions to be used as IDictionary keys.
    /// </summary>
    public class WavefrontGaugeOptions : GaugeOptions
    {
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Context == null ? 0 : Context.GetHashCode());
                hash = hash * 23 + Name.GetHashCode();
                hash = hash * 23 + Tags.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !GetType().Equals(obj.GetType()))
            {
                return false;
            }
            var other = (WavefrontGaugeOptions)obj;
            return Context.Equals(other.Context) && Name.Equals(other.Name)
                          && Tags.Equals(other.Tags);
        }
    }
}
