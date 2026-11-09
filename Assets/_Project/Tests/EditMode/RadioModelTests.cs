using System.Linq;
using NUnit.Framework;

namespace WhiteRoom.Radio.Tests
{
    public class RadioMathTests
    {
        [TestCase(7, 5.47f)]
        [TestCase(12, 0.293f)]
        public void LoRaRate_MatchesDatasheet(int sf, float expectedKbps)
        {
            Assert.That(RadioMath.LoRaRateKbps(sf), Is.EqualTo(expectedKbps).Within(0.01f));
        }

        [Test]
        public void LoRa_HigherSpreadingFactor_TradesRateForRange()
        {
            var sf7 = new RadioConfig(HeistMission.LoRa, spreadingFactor: 7);
            var sf12 = new RadioConfig(HeistMission.LoRa, spreadingFactor: 12);

            Assert.That(RadioMath.RateKbps(sf12), Is.LessThan(RadioMath.RateKbps(sf7)));
            Assert.That(RadioMath.EffectiveRangeM(sf12, 0), Is.GreaterThan(RadioMath.EffectiveRangeM(sf7, 0)));
        }

        [Test]
        public void Buildings_ShrinkRange()
        {
            var ble = new RadioConfig(HeistMission.Ble);
            Assert.That(RadioMath.EffectiveRangeM(ble, 1), Is.EqualTo(20f).Within(0.01f));
        }

        [Test]
        public void BatteryLife_IsEnergyOverAveragePower()
        {
            Assert.That(RadioMath.BatteryLifeHours(220, 110), Is.EqualTo(2f));
        }
    }

    /// <summary>
    /// A tabela de resposta da Missão 1: cada enlace tem uma tecnologia certa,
    /// o celular funciona onde há cobertura mas deixa rastro.
    /// </summary>
    public class HeistMissionTests
    {
        static LinkResult Eval(LinkRequirement link, RadioConfig config) =>
            LinkEvaluator.Evaluate(link, HeistMission.DefaultGeometry(link), config);

        static CheckKind[] FailedKinds(LinkResult r) => r.Failures.Select(c => c.kind).ToArray();

        [Test]
        public void VaultVoice_OnlyBleWorks()
        {
            var link = HeistMission.VaultVoice;
            Assert.That(Eval(link, new RadioConfig(HeistMission.Ble)).Works, Is.True);
            Assert.That(FailedKinds(Eval(link, new RadioConfig(HeistMission.WiFi))), Is.EquivalentTo(new[] { CheckKind.Battery }));
            Assert.That(FailedKinds(Eval(link, new RadioConfig(HeistMission.LoRa, spreadingFactor: 7))), Is.EquivalentTo(new[] { CheckKind.Rate }));
            Assert.That(FailedKinds(Eval(link, new RadioConfig(HeistMission.Cellular))), Is.EquivalentTo(new[] { CheckKind.Coverage }));
        }

        [Test]
        public void HiddenSensors_LoRaNeedsSf12()
        {
            var link = HeistMission.HiddenSensors;
            var sf12 = Eval(link, new RadioConfig(HeistMission.LoRa, spreadingFactor: 12));
            Assert.That(sf12.Works, Is.True);
            Assert.That(sf12.Traced, Is.False);

            Assert.That(FailedKinds(Eval(link, new RadioConfig(HeistMission.LoRa, spreadingFactor: 11))), Is.EquivalentTo(new[] { CheckKind.Range }));
            Assert.That(FailedKinds(Eval(link, new RadioConfig(HeistMission.Ble))), Does.Contain(CheckKind.Range));
            Assert.That(FailedKinds(Eval(link, new RadioConfig(HeistMission.WiFi))), Is.EquivalentTo(new[] { CheckKind.Range, CheckKind.Battery }));
        }

        [Test]
        public void HiddenSensors_RejectsDirectionalAntenna()
        {
            var r = Eval(HeistMission.HiddenSensors, new RadioConfig(HeistMission.LoRa, directionalAntenna: true, spreadingFactor: 7));
            Assert.That(FailedKinds(r), Is.EquivalentTo(new[] { CheckKind.Antenna }));
        }

        [Test]
        public void DroneVideo_WiFiNeedsDirectionalAntenna()
        {
            var link = HeistMission.DroneVideo;
            Assert.That(Eval(link, new RadioConfig(HeistMission.WiFi, directionalAntenna: true)).Works, Is.True);
            Assert.That(FailedKinds(Eval(link, new RadioConfig(HeistMission.WiFi))), Is.EquivalentTo(new[] { CheckKind.Range }));
            Assert.That(FailedKinds(Eval(link, new RadioConfig(HeistMission.Ble, directionalAntenna: true))), Is.EquivalentTo(new[] { CheckKind.Range, CheckKind.Rate }));
            Assert.That(FailedKinds(Eval(link, new RadioConfig(HeistMission.LoRa, spreadingFactor: 7))), Is.EquivalentTo(new[] { CheckKind.Rate }));
        }

        [Test]
        public void Cellular_WorksWithCoverageButIsTraced()
        {
            foreach (var link in new[] { HeistMission.HiddenSensors, HeistMission.DroneVideo })
            {
                var r = Eval(link, new RadioConfig(HeistMission.Cellular));
                Assert.That(r.Works, Is.True, link.id);
                Assert.That(r.Traced, Is.True, link.id);
            }
        }

        [Test]
        public void Heist_Outcomes()
        {
            var clean = HeistEvaluator.Evaluate(new[]
            {
                Plan(HeistMission.VaultVoice, new RadioConfig(HeistMission.Ble)),
                Plan(HeistMission.HiddenSensors, new RadioConfig(HeistMission.LoRa, spreadingFactor: 12)),
                Plan(HeistMission.DroneVideo, new RadioConfig(HeistMission.WiFi, directionalAntenna: true)),
            });
            Assert.That(clean.Outcome, Is.EqualTo(HeistOutcome.Success));

            var traced = HeistEvaluator.Evaluate(new[]
            {
                Plan(HeistMission.VaultVoice, new RadioConfig(HeistMission.Ble)),
                Plan(HeistMission.HiddenSensors, new RadioConfig(HeistMission.Cellular)),
                Plan(HeistMission.DroneVideo, new RadioConfig(HeistMission.WiFi, directionalAntenna: true)),
            });
            Assert.That(traced.Outcome, Is.EqualTo(HeistOutcome.SuccessButTraced));

            var failed = HeistEvaluator.Evaluate(new[]
            {
                Plan(HeistMission.VaultVoice, new RadioConfig(HeistMission.WiFi)),
                Plan(HeistMission.HiddenSensors, new RadioConfig(HeistMission.LoRa, spreadingFactor: 12)),
                Plan(HeistMission.DroneVideo, new RadioConfig(HeistMission.WiFi, directionalAntenna: true)),
            });
            Assert.That(failed.Outcome, Is.EqualTo(HeistOutcome.Failed));
        }

        [Test]
        public void FailureMessage_ExplainsWithNumbers()
        {
            var r = Eval(HeistMission.VaultVoice, new RadioConfig(HeistMission.WiFi));
            var battery = r.Failures.Single();
            Assert.That(battery.message, Does.StartWith("A bateria acaba em 53 min, faltam 1 h 7 min."));
        }

        static (LinkRequirement, LinkGeometry, RadioConfig) Plan(LinkRequirement link, RadioConfig config) =>
            (link, HeistMission.DefaultGeometry(link), config);
    }
}
