using System;

namespace WhiteRoom.Radio
{
    /// <summary>
    /// Ficha técnica de uma tecnologia de rádio, com valores simplificados de faixas reais.
    /// Potências em mW, alcance em metros, taxa em kbps.
    /// </summary>
    [Serializable]
    public class RadioSpec
    {
        public string id;
        public string displayName;

        /// <summary>Alcance urbano sem obstáculos, com antena omnidirecional. Para LoRa, vale no SF7.</summary>
        public float baseRangeM;

        /// <summary>Taxa útil máxima. Ignorada quando <see cref="usesSpreadingFactor"/> é verdadeiro.</summary>
        public float rateKbps;

        /// <summary>Potência gasta enquanto transmite.</summary>
        public float txPowerMw;

        /// <summary>Potência para manter o enlace vivo entre transmissões (conexão, repouso).</summary>
        public float idlePowerMw;

        /// <summary>Fração do alcance que sobra a cada prédio atravessado (0 a 1).</summary>
        public float buildingKeep = 1f;

        /// <summary>Usa a rede de uma operadora: ignora distância, mas exige cobertura nas duas pontas.</summary>
        public bool usesInfrastructure;

        /// <summary>A conexão fica registrada e a vigilância consegue ver.</summary>
        public bool traceable;

        /// <summary>Taxa e alcance dependem do fator de espalhamento (LoRa).</summary>
        public bool usesSpreadingFactor;
    }

    /// <summary>Como o jogador configurou um rádio num enlace.</summary>
    [Serializable]
    public struct RadioConfig
    {
        public RadioSpec spec;
        public bool directionalAntenna;

        /// <summary>Fator de espalhamento do LoRa, de 7 a 12.</summary>
        public int spreadingFactor;

        public RadioConfig(RadioSpec spec, bool directionalAntenna = false, int spreadingFactor = RadioMath.MinSpreadingFactor)
        {
            this.spec = spec;
            this.directionalAntenna = directionalAntenna;
            this.spreadingFactor = spreadingFactor;
        }
    }
}
