using System;
namespace PressureFilterSimulator.Simulation;

public sealed class PressureSignalGenerator
{
    private readonly Random _random;

    private bool _pressureReachedMaximum;
    private int _holdCyclesRemaining;

    private double _disturbance;
    private int _disturbanceCyclesRemaining;

    public double CycleSeconds { get; }

    public double MaxPressure { get; set; } = 5000.0;
    public double RampRate { get; set; } = 25.0;

    public double MaxDisturbance { get; set; } = 500.0;
    // Средняя частота возникновения помех, событий в секунду.
    public double NoiseRate { get; set; } = 3.0;

    public double BackgroundNoise { get; set; } = 5.0;

    public double TruePressure { get; private set; }
    public double RawPressure { get; private set; }

    public PressureSignalGenerator(
        double startPressure = 0.0,
        double cycleSeconds = 0.01,
        int seed = 12345)
    {
        TruePressure = startPressure;
        CycleSeconds = cycleSeconds;
        _random = new Random(seed);
    }

    public double Update()
    {
        UpdateProcess();
        UpdateDisturbance();

        double backgroundNoise =
            RandomRange(-BackgroundNoise, BackgroundNoise);

        RawPressure =
            TruePressure +
            _disturbance +
            backgroundNoise;

        RawPressure = Math.Max(0.0, RawPressure);

        return RawPressure;
    }

    private void UpdateProcess()
    {
        if (!_pressureReachedMaximum)
        {
            TruePressure += RampRate * CycleSeconds;

            if (TruePressure >= MaxPressure)
            {
                TruePressure = MaxPressure;
                _pressureReachedMaximum = true;

                // Выдержка 2...5 секунд
                _holdCyclesRemaining = _random.Next(200, 501);
            }

            return;
        }

        if (_holdCyclesRemaining > 0)
        {
            _holdCyclesRemaining--;
            return;
        }

        // Сброс давления и начало нового цикла
        TruePressure = 0.0;
        _pressureReachedMaximum = false;
    }

    private void UpdateDisturbance()
    {
        if (_disturbanceCyclesRemaining > 0)
        {
            _disturbanceCyclesRemaining--;
            return;
        }

        _disturbance = 0.0;

        // Переводим частоту событий/сек
        // в вероятность события за один цикл.
        double probabilityPerCycle =
            NoiseRate * CycleSeconds;

        probabilityPerCycle =
            Math.Clamp(probabilityPerCycle, 0.0, 1.0);

        if (_random.NextDouble() >= probabilityPerCycle)
            return;

        _disturbance =
            RandomRange(-MaxDisturbance, MaxDisturbance);

        // 20...500 ms при цикле 10 ms
        _disturbanceCyclesRemaining =
            _random.Next(2, 51);
    }

    private double RandomRange(double min, double max)
    {
        return min + _random.NextDouble() * (max - min);
    }
}