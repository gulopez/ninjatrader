using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;

using NinjaTrader.NinjaScript.Utilities;

public class BreakEvenExitStrategy
{
    private readonly Strategy _strategy;
    private int _triggerState;
    private double _stopPrice;
    private double _triggerPrice;
    private bool _IsVerbose = false;

    public double StopPrice { get => _stopPrice; set => _stopPrice = value; }
    public double TriggerPrice { get => _triggerPrice; set => _triggerPrice = value; }
    public int TriggerState { get => _triggerState; set => _triggerState = value; }
    public bool IsVerbose { get => _IsVerbose; set => _IsVerbose = value; }

    private void PrintOutput(string text)
    {
        if (IsVerbose)
        {
            _strategy.Print(string.Format("{0},{1},{2},{3},{4}", _strategy.Time[0], text, TriggerState, TriggerPrice, StopPrice));
        }

    }
    public BreakEvenExitStrategy(Strategy strategy)
    {
        _strategy = strategy;
        TriggerState = 0;
        StopPrice = 0;
        TriggerPrice = 0;
    }
    public void Process()
    {
        // Set 1 - Reset state when flat
        if ((TriggerState >= 2) && (_strategy.Position.MarketPosition == MarketPosition.Flat))
        {
            TriggerState = 0;
        }

        // Set 3 - Initialize when entering long position
        if ((TriggerState == 1) && (_strategy.Position.MarketPosition == MarketPosition.Long))
        {
            TriggerState = 2;
            PrintOutput("Long position entered");
        }

        // Set 4 - Move to break-even when price reaches trigger
        if ((TriggerState == 2) && (_strategy.Close[0] >= TriggerPrice))
        {
            TriggerState = 3;
            StopPrice = TriggerPrice > _strategy.Low[0] ? TriggerPrice : _strategy.Low[0];
            PrintOutput("Close > Trigger Price");
            //_strategy.Draw.Diamond(_strategy, @"BreakEvenBuilderExample Diamond_1", true, 0, (_strategy.High[0] + (2 * _strategy.TickSize)), Brushes.DarkCyan);
        }

        // Set 5 - Update trailing stop
        if (TriggerState >= 2)
        {
            if (TriggerState == 3)
            {
                PrintOutput("Updating trailing stop");
                StopPrice = _strategy.Low[0];
            }

            if (_strategy.Close[0] < StopPrice)
            {
                //Exit right away
                PrintOutput("Exiting strong reversal");
                _strategy.ExitLong(@"entry");
            }
            else
            {
                _strategy.ExitLongStopMarket(Convert.ToInt32(_strategy.DefaultQuantity),
                   StopPrice, @"exit", @"entry");

            }

        }
    }

}