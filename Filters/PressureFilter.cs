using System;

namespace PressureFilterSimulator.Filters;

public sealed class PressureFilter
{
    private double _alpha = 0.05;

    private double _stage1;
    private double _stage2;

    private bool _initialized;
    private bool _enabled = true;

    public double Stage1 => _stage1;
    public double Stage2 => _stage2;
    public bool Initialized => _initialized;

    public bool Enabled
    {
        get => _enabled;

        set
        {
            if (_enabled == value)
                return;

            _enabled = value;

            // При любом переключении сбрасываем внутреннее состояние.
            // После повторного включения фильтр стартует
            // от текущего входного значения.
            Reset();
        }
    }

    public double Alpha
    {
        get => _alpha;

        set
        {
            double newAlpha = Math.Clamp(value, 0.001, 1.0);

            if (Math.Abs(_alpha - newAlpha) < 0.000001)
                return;

            _alpha = newAlpha;

            // Новый коэффициент -> начинаем фильтрацию заново
            Reset();
        }
    }

    public double Update(double input)
    {
        // Bypass
        if (!Enabled)
            return input;

        // Инициализация текущим RAW
        if (!_initialized)
        {
            _stage1 = input;
            _stage2 = input;

            _initialized = true;

            return input;
        }

        // EMA #1
        _stage1 =
            Alpha * input +
            (1.0 - Alpha) * _stage1;

        // EMA #2
        _stage2 =
            Alpha * _stage1 +
            (1.0 - Alpha) * _stage2;

        return _stage2;
    }

    public void Reset()
    {
        _stage1 = 0.0;
        _stage2 = 0.0;

        _initialized = false;
    }
}