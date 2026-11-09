using System.Collections.Generic;
using System.Linq;

namespace WhiteRoom.Radio
{
    /// <summary>Dados padrão da Missão 1 (rádio do roubo). Fonte única para testes, assets e cena.</summary>
    public static class HeistMission
    {
        public static readonly RadioSpec Ble = new RadioSpec
        {
            id = "ble", displayName = "BLE 5",
            baseRangeM = 40, rateKbps = 1_400, txPowerMw = 15, idlePowerMw = 1, buildingKeep = 0.5f,
        };

        public static readonly RadioSpec WiFi = new RadioSpec
        {
            id = "wifi", displayName = "Wi-Fi 6",
            baseRangeM = 70, rateKbps = 200_000, txPowerMw = 800, idlePowerMw = 250, buildingKeep = 0.6f,
        };

        public static readonly RadioSpec LoRa = new RadioSpec
        {
            id = "lora", displayName = "LoRa",
            baseRangeM = 1_500, txPowerMw = 120, idlePowerMw = 0.005f, buildingKeep = 0.8f,
            usesSpreadingFactor = true,
        };

        public static readonly RadioSpec Cellular = new RadioSpec
        {
            id = "cell", displayName = "Celular (5G)",
            rateKbps = 200_000, txPowerMw = 1_500, idlePowerMw = 5,
            usesInfrastructure = true, traceable = true,
        };

        public static IReadOnlyList<RadioSpec> Radios { get; } = new[] { Ble, WiFi, LoRa, Cellular };

        public static readonly LinkRequirement VaultVoice = new LinkRequirement
        {
            id = "a-voice", displayName = "A · Voz no cofre",
            description = "Três membros falando entre si no subsolo do banco.",
            requiredKbps = 32, durationHours = 2, batteryMwh = 220, batteryName = "Fone de ouvido",
            operatorCoverage = false, allowsDirectional = false,
        };

        public static readonly LinkRequirement HiddenSensors = new LinkRequirement
        {
            id = "b-sensors", displayName = "B · Sensores escondidos",
            description = "Status das portas a cada 10 s, do prédio até a van.",
            requiredKbps = 0.016f, durationHours = 72, batteryMwh = 675, batteryName = "Bateria de moeda",
            operatorCoverage = true, allowsDirectional = false,
        };

        public static readonly LinkRequirement DroneVideo = new LinkRequirement
        {
            id = "c-drone", displayName = "C · Vídeo do drone",
            description = "Câmera de vigia em 1080p, do drone até a van.",
            requiredKbps = 5_000, durationHours = 0.75f, batteryMwh = 50_000, batteryName = "Bateria do drone",
            operatorCoverage = true, allowsDirectional = true,
        };

        public static IReadOnlyList<LinkRequirement> Links { get; } = new[] { VaultVoice, HiddenSensors, DroneVideo };

        /// <summary>Geometria usada até o mapa (P2) existir.</summary>
        public static LinkGeometry DefaultGeometry(LinkRequirement link) => link.id switch
        {
            "a-voice" => new LinkGeometry(30, 0),
            "b-sensors" => new LinkGeometry(2_400, 2),
            "c-drone" => new LinkGeometry(300, 0),
            _ => new LinkGeometry(0, 0),
        };
    }

    public enum HeistOutcome { Failed, Success, SuccessButTraced }

    public class HeistResult
    {
        public readonly List<LinkResult> links = new List<LinkResult>();

        public HeistOutcome Outcome
        {
            get
            {
                if (links.Any(l => !l.Works)) return HeistOutcome.Failed;
                return links.Any(l => l.Traced) ? HeistOutcome.SuccessButTraced : HeistOutcome.Success;
            }
        }
    }

    public static class HeistEvaluator
    {
        public static HeistResult Evaluate(IEnumerable<(LinkRequirement link, LinkGeometry geometry, RadioConfig config)> plan)
        {
            var result = new HeistResult();
            foreach (var (link, geometry, config) in plan)
                result.links.Add(LinkEvaluator.Evaluate(link, geometry, config));
            return result;
        }
    }
}
