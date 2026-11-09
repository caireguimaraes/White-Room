using System;

namespace WhiteRoom.Radio
{
    /// <summary>Contas do modelo de rádio. Sem dependência de cena para poder testar em EditMode.</summary>
    public static class RadioMath
    {
        public const int MinSpreadingFactor = 7;
        public const int MaxSpreadingFactor = 12;

        /// <summary>Ganho de alcance da antena direcional (aprox. +13 dB num ambiente urbano).</summary>
        public const float DirectionalGain = 4.5f;

        const double LoRaBandwidthHz = 125_000;
        const double LoRaCodingRate = 4.0 / 5.0;

        // Cada passo de SF ganha ~2,5 dB de sensibilidade. Com expoente de perda urbano n = 3,
        // o alcance cresce 10^(2,5 / (10 * 3)) por passo.
        static readonly double RangePerSfStep = Math.Pow(10, 2.5 / 30.0);

        /// <summary>Taxa do LoRa em kbps: Rb = SF · (BW / 2^SF) · CR.</summary>
        public static float LoRaRateKbps(int sf)
        {
            sf = ClampSf(sf);
            return (float)(sf * (LoRaBandwidthHz / Math.Pow(2, sf)) * LoRaCodingRate / 1000.0);
        }

        public static float RateKbps(in RadioConfig config)
        {
            return config.spec.usesSpreadingFactor ? LoRaRateKbps(config.spreadingFactor) : config.spec.rateKbps;
        }

        /// <summary>Alcance depois da antena e dos prédios no caminho.</summary>
        public static float EffectiveRangeM(in RadioConfig config, int buildingsCrossed)
        {
            var spec = config.spec;
            double range = spec.baseRangeM;
            if (spec.usesSpreadingFactor)
                range *= Math.Pow(RangePerSfStep, ClampSf(config.spreadingFactor) - MinSpreadingFactor);
            if (config.directionalAntenna)
                range *= DirectionalGain;
            range *= Math.Pow(spec.buildingKeep, Math.Max(0, buildingsCrossed));
            return (float)range;
        }

        /// <summary>Fração do tempo transmitindo para dar conta da taxa pedida.</summary>
        public static float DutyCycle(float requiredKbps, float rateKbps)
        {
            if (rateKbps <= 0) return 1f;
            return Math.Min(1f, requiredKbps / rateKbps);
        }

        public static float AveragePowerMw(in RadioConfig config, float requiredKbps)
        {
            float duty = DutyCycle(requiredKbps, RateKbps(config));
            return config.spec.idlePowerMw + config.spec.txPowerMw * duty;
        }

        public static float BatteryLifeHours(float batteryMwh, float averagePowerMw)
        {
            if (averagePowerMw <= 0) return float.PositiveInfinity;
            return batteryMwh / averagePowerMw;
        }

        public static int ClampSf(int sf) => Math.Clamp(sf, MinSpreadingFactor, MaxSpreadingFactor);
    }
}
