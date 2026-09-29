using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PressureFilterSimulator.Filters;
using PressureFilterSimulator.Simulation;

namespace PressureFilterSimulator;

public partial class MainWindow : Window
{
    private const double CycleSeconds = 0.01;
    private const int MaxSamples = 3000;

    private readonly DispatcherTimer _timer;

    private readonly PressureSignalGenerator _generator;
    private readonly PressureFilter _filter;

    private readonly List<double> _time = new();
    private readonly List<double> _rawPressure = new();
    private readonly List<double> _filteredPressure = new();

    private ScottPlot.Plottables.Scatter _rawPlot;
    private ScottPlot.Plottables.Scatter _filteredPlot;

    private double _simulationTime;

    public MainWindow()
    {
        InitializeComponent();

        // ------------------------------------------------------------
        // Генератор сигнала
        // ------------------------------------------------------------

        _generator = new PressureSignalGenerator(
            startPressure: 0.0,
            cycleSeconds: CycleSeconds);

        // ------------------------------------------------------------
        // Тестируемый фильтр
        // ------------------------------------------------------------

        _filter = new PressureFilter();

        ActualAlphaText.Text = _filter.Alpha.ToString("0.###");

        // ------------------------------------------------------------
        // Верхний график: RAW
        // ------------------------------------------------------------

        _rawPlot = PressurePlot.Plot.Add.Scatter(
            _time.ToArray(),
            _rawPressure.ToArray());

        _rawPlot.LegendText = "RAW pressure";

        PressurePlot.Plot.Title("RAW Pressure");
        PressurePlot.Plot.XLabel("Time, s");
        PressurePlot.Plot.YLabel("Pressure, PSI");

        PressurePlot.Plot.Axes.SetLimitsY(0, 6000);
        PressurePlot.Plot.ShowLegend();

        // ------------------------------------------------------------
        // Нижний график: FILTERED
        // ------------------------------------------------------------

        _filteredPlot = FilteredPlot.Plot.Add.Scatter(
            _time.ToArray(),
            _filteredPressure.ToArray());

        _filteredPlot.LegendText = "FILTERED pressure";

        FilteredPlot.Plot.Title("Filtered Pressure");
        FilteredPlot.Plot.XLabel("Time, s");
        FilteredPlot.Plot.YLabel("Pressure, PSI");

        FilteredPlot.Plot.Axes.SetLimitsY(0, 6000);
        FilteredPlot.Plot.ShowLegend();

        // ------------------------------------------------------------
        // Таймер симуляции
        // ------------------------------------------------------------

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(10)
        };

        _timer.Tick += Timer_Tick;

        // Синхронизируем ось времени двух графиков.
        // Масштабирование и перемещение одного графика
        // автоматически передаётся второму.
        PressurePlot.Plot.Axes.Link(
            FilteredPlot,
            x: true,
            y: false);

        FilteredPlot.Plot.Axes.Link(
            PressurePlot,
            x: true,
            y: false);
    }

    private void StartButton_Click(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(NoiseTextBox.Text, out double maxNoise))
            _generator.MaxDisturbance = maxNoise;

        _timer.Start();
    }

    private void StopButton_Click(object? sender, RoutedEventArgs e)
    {
        _timer.Stop();
    }

    private void ApplyAlphaButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        string text = AlphaTextBox.Text?
            .Trim()
            .Replace(',', '.') ?? "";

        if (!double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double alpha))
        {
            AlphaTextBox.Text =
                _filter.Alpha.ToString("0.###", CultureInfo.InvariantCulture);

            return;
        }

        _filter.Alpha = alpha;

        AlphaTextBox.Text =
            _filter.Alpha.ToString("0.###", CultureInfo.InvariantCulture);

        ActualAlphaText.Text =
            _filter.Alpha.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {

        // ------------------------------------------------------------
        // Настройки фильтра из UI
        // ------------------------------------------------------------

        _filter.Enabled =
            FilterEnabledCheckBox.IsChecked == true;


        // ------------------------------------------------------------
        // 1. Генерируем RAW
        // ------------------------------------------------------------

        double raw = _generator.Update();

        // ------------------------------------------------------------
        // 2. Пропускаем через тестируемый фильтр
        // ------------------------------------------------------------

        double filtered = _filter.Update(raw);

        RawValueText.Text = raw.ToString("F1");
        Stage1ValueText.Text = _filter.Stage1.ToString("F1");
        Stage2ValueText.Text = _filter.Stage2.ToString("F1");

        // ------------------------------------------------------------
        // 3. Сохраняем данные
        // ------------------------------------------------------------

        _time.Add(_simulationTime);
        _rawPressure.Add(raw);
        _filteredPressure.Add(filtered);

        _simulationTime += CycleSeconds;

        // ------------------------------------------------------------
        // 4. Храним последние 30 секунд
        // ------------------------------------------------------------

        if (_time.Count > MaxSamples)
        {
            _time.RemoveAt(0);
            _rawPressure.RemoveAt(0);
            _filteredPressure.RemoveAt(0);
        }

        // ------------------------------------------------------------
        // 5. Обновляем графики
        // ------------------------------------------------------------

        UpdatePlots();
    }

    private void UpdatePlots()
    {
        // RAW

        PressurePlot.Plot.Remove(_rawPlot);

        _rawPlot = PressurePlot.Plot.Add.Scatter(
            _time.ToArray(),
            _rawPressure.ToArray());

        _rawPlot.LegendText = "RAW pressure";

        // FILTERED

        FilteredPlot.Plot.Remove(_filteredPlot);

        _filteredPlot = FilteredPlot.Plot.Add.Scatter(
            _time.ToArray(),
            _filteredPressure.ToArray());

        _filteredPlot.LegendText = "FILTERED pressure";

        // Одинаковая временная шкала

        if (_simulationTime > 30.0)
        {
            PressurePlot.Plot.Axes.SetLimitsX(
                _simulationTime - 30.0,
                _simulationTime);
        }

        PressurePlot.Refresh();
        FilteredPlot.Refresh();
    }
}