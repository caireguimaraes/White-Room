using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WhiteRoom.Radio
{
    public enum CheckKind { Coverage, Antenna, Range, Rate, Battery, Trace }

    public readonly struct LinkCheck
    {
        public readonly CheckKind kind;
        public readonly bool passed;

        /// <summary>Falha que não impede o enlace de funcionar, mas tem consequência (rastro).</summary>
        public readonly bool isWarning;
        public readonly float required;
        public readonly float actual;
        public readonly string message;

        public LinkCheck(CheckKind kind, bool passed, float required, float actual, string message, bool isWarning = false)
        {
            this.kind = kind;
            this.passed = passed;
            this.required = required;
            this.actual = actual;
            this.message = message;
            this.isWarning = isWarning;
        }
    }

    public class LinkResult
    {
        public LinkRequirement requirement;
        public RadioConfig config;
        public float effectiveRangeM;
        public float rateKbps;
        public float averagePowerMw;
        public float batteryLifeHours;
        public readonly List<LinkCheck> checks = new List<LinkCheck>();

        public bool Works => checks.All(c => c.passed || c.isWarning);
        public bool Traced => checks.Any(c => c.kind == CheckKind.Trace && !c.passed);
        public IEnumerable<LinkCheck> Failures => checks.Where(c => !c.passed && !c.isWarning);
    }

    /// <summary>Decide se um rádio configurado dá conta de um enlace, e explica por quê.</summary>
    public static class LinkEvaluator
    {
        static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        public static LinkResult Evaluate(LinkRequirement link, LinkGeometry geometry, RadioConfig config)
        {
            var spec = config.spec;
            var result = new LinkResult
            {
                requirement = link,
                config = config,
                rateKbps = RadioMath.RateKbps(config),
                averagePowerMw = RadioMath.AveragePowerMw(config, link.requiredKbps),
            };
            result.batteryLifeHours = RadioMath.BatteryLifeHours(link.batteryMwh, result.averagePowerMw);

            if (spec.usesInfrastructure)
            {
                result.effectiveRangeM = float.PositiveInfinity;
                result.checks.Add(link.operatorCoverage
                    ? new LinkCheck(CheckKind.Coverage, true, 1, 1, "Há sinal da operadora nas duas pontas.")
                    : new LinkCheck(CheckKind.Coverage, false, 1, 0, "Sem sinal da operadora aqui. Precisa de uma torre alcançável nas duas pontas."));
            }
            else
            {
                if (config.directionalAntenna && !link.allowsDirectional)
                    result.checks.Add(new LinkCheck(CheckKind.Antenna, false, 0, 1,
                        "Não cabe antena direcional neste enlace."));

                result.effectiveRangeM = RadioMath.EffectiveRangeM(config, geometry.buildingsCrossed);
                bool rangeOk = result.effectiveRangeM >= geometry.distanceM;
                result.checks.Add(new LinkCheck(CheckKind.Range, rangeOk, geometry.distanceM, result.effectiveRangeM, rangeOk
                    ? $"Alcance de {Meters(result.effectiveRangeM)} cobre {Meters(geometry.distanceM)}."
                    : $"Alcance de {Meters(result.effectiveRangeM)} não chega a {Meters(geometry.distanceM)}{BuildingsNote(geometry.buildingsCrossed)}."));
            }

            bool rateOk = result.rateKbps >= link.requiredKbps;
            result.checks.Add(new LinkCheck(CheckKind.Rate, rateOk, link.requiredKbps, result.rateKbps, rateOk
                ? $"Taxa de {Rate(result.rateKbps)} dá conta de {Rate(link.requiredKbps)}."
                : $"Taxa de {Rate(result.rateKbps)} é menor que os {Rate(link.requiredKbps)} necessários."));

            // Sem taxa suficiente o rádio transmitiria sem parar; a falha que importa é a de taxa.
            bool batteryOk = result.batteryLifeHours >= link.durationHours;
            if (rateOk) result.checks.Add(new LinkCheck(CheckKind.Battery, batteryOk, link.durationHours, result.batteryLifeHours, batteryOk
                ? $"A bateria dura {Hours(result.batteryLifeHours)} gastando {Power(result.averagePowerMw)}."
                : $"A bateria acaba em {Hours(result.batteryLifeHours)}, faltam {Hours(link.durationHours - result.batteryLifeHours)}. Consumo médio: {Power(result.averagePowerMw)}."));

            if (spec.traceable)
                result.checks.Add(new LinkCheck(CheckKind.Trace, false, 0, 1,
                    "A conexão passa pela operadora e fica registrada. A vigilância consegue ver.", isWarning: true));

            return result;
        }

        static string BuildingsNote(int buildings) => buildings switch
        {
            <= 0 => "",
            1 => " (1 prédio no caminho)",
            _ => $" ({buildings} prédios no caminho)",
        };

        public static string Meters(float m) => m >= 1000
            ? (m / 1000f).ToString("0.0", PtBr) + " km"
            : m.ToString("0", PtBr) + " m";

        public static string Rate(float kbps)
        {
            if (kbps >= 1000) return (kbps / 1000f).ToString("0.#", PtBr) + " Mbps";
            if (kbps >= 1) return kbps.ToString("0.#", PtBr) + " kbps";
            return (kbps * 1000f).ToString("0", PtBr) + " bps";
        }

        public static string Power(float mw) => mw >= 1000
            ? (mw / 1000f).ToString("0.0", PtBr) + " W"
            : mw.ToString(mw < 10 ? "0.0" : "0", PtBr) + " mW";

        public static string Hours(float h)
        {
            if (float.IsPositiveInfinity(h)) return "para sempre";
            int minutes = (int)Math.Round(h * 60f);
            if (minutes < 60) return minutes + " min";
            if (h >= 10) return h.ToString("0", PtBr) + " h";
            return minutes % 60 == 0 ? $"{minutes / 60} h" : $"{minutes / 60} h {minutes % 60} min";
        }
    }
}
