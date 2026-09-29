namespace PressureFilterSimulator.Models;

public sealed class FilterConfig
{
    // PLC cycle
    public double CycleMs { get; set; } = 10.0;

    // Pressure sensor
    public double CurrentMin { get; set; } = 4.0;
    public double CurrentMax { get; set; } = 20.0;

    public double PressureMin { get; set; } = 0.0;
    public double PressureMax { get; set; } = 15000.0;

    // Adaptive EMA
    public double AlphaSlow { get; set; } = 0.05;
    public double AlphaFast { get; set; } = 0.18;

    public double DeltaThreshold { get; set; } = 0.15;
    public double DeltaHysteresis { get; set; } = 0.05;

    public double PressureToCurrent(double pressure)
    {
        return CurrentMin
             + (pressure - PressureMin)
             / (PressureMax - PressureMin)
             * (CurrentMax - CurrentMin);
    }

    public double CurrentToPressure(double current)
    {
        return (current - CurrentMin)
             / (CurrentMax - CurrentMin)
             * (PressureMax - PressureMin)
             + PressureMin;
    }
}