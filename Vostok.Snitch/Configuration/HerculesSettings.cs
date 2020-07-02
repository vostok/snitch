namespace Vostok.Snitch.Configuration
{
    public class HerculesSettings
    {
        public double BoostLocalDatacenterMultiplier { get; set; } = 20;

        public double MinimumWeightForBoostingLocalDatacenter { get; set; } = 0;

        public string HerculesStreamApiTopology { get; set; } = "Hercules.StreamApi";
        public string HerculesGateTopology { get; set; } = "Hercules.Gate";
        public string HerculesEnvironmentTopology { get; set; } = "default";
    }
}