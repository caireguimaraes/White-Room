using System;

namespace WhiteRoom.Radio
{
    /// <summary>O que um enlace do roubo precisa. Não depende de onde o jogador pôs os rádios.</summary>
    [Serializable]
    public class LinkRequirement
    {
        public string id;
        public string displayName;
        public string description;

        public float requiredKbps;
        public float durationHours;
        public float batteryMwh;
        public string batteryName;

        /// <summary>Existe sinal de operadora nas duas pontas (falso num subsolo blindado).</summary>
        public bool operatorCoverage = true;

        /// <summary>Cabe uma antena direcional (falso para sensores escondidos e pessoas andando).</summary>
        public bool allowsDirectional;
    }

    /// <summary>A geometria do enlace no mapa: vem do posicionamento (P2) ou dos valores padrão da missão.</summary>
    [Serializable]
    public struct LinkGeometry
    {
        public float distanceM;
        public int buildingsCrossed;

        public LinkGeometry(float distanceM, int buildingsCrossed)
        {
            this.distanceM = distanceM;
            this.buildingsCrossed = buildingsCrossed;
        }
    }
}
