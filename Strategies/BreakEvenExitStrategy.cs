using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Windows.Media;

public class BreakEvenExitStrategy
{
    private readonly Strategy _strategy;
    private int _triggerState;
    private double _stopPrice;
    private double _triggerPrice;

    public BreakEvenExitStrategy(Strategy strategy)
    {
        _strategy = strategy;
        _triggerState = 0;
        _stopPrice = 0;
        _triggerPrice = 0;
    }

    public void SetTriggerPrice(double price)
    {
        _triggerPrice = price;
    }

    public void SetStopPrice(double price)
    {
        _stopPrice = price;
    }

    public void Process()
    {
        // Set 1 - Reset state when flat
        if ((_triggerState >= 2) && (_strategy.Position.MarketPosition == MarketPosition.Flat))
        {
            _triggerState = 0;
        }

        // Set 3 - Initialize when entering long position
        if ((_triggerState == 1) && (_strategy.Position.MarketPosition == MarketPosition.Long))
        {
            _triggerState = 2;
        }

        // Set 4 - Move to break-even when price reaches trigger
        if ((_triggerState == 2) && (_strategy.Close[0] >= _triggerPrice))
        {
            _triggerState = 3;
            _stopPrice = _strategy.Low[0];
            _strategy.Draw.Diamond(_strategy, @"BreakEvenBuilderExample Diamond_1", true, 0, 
                (_strategy.High[0] + (2 * _strategy.TickSize)), Brushes.DarkCyan);
        }

        // Set 5 - Update trailing stop
        if (_triggerState >= 2)
        {
            if (_triggerState == 3)
            {
                _stopPrice = _strategy.Low[0];
            }

            _strategy.ExitLongStopMarket(Convert.ToInt32(_strategy.DefaultQuantity), 
                _stopPrice, @"exit", @"entry");
        }
    }

    public void SetTriggerState(int state)
    {
        _triggerState = state;
    }
}