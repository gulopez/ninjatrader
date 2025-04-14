#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class EMASlopeColor : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Exponential Moving Average. The Exponential Moving Average is an indicator that shows the average value of a security's price over a period of time. When calculating a moving average. The EMA_Slope_Color applies more weight to recent prices than the SMA.  Colors based on IsRising or IsFalling";
				Name										= "EMASlopeColor";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				Period										= 14;
				UpColor										= Brushes.LimeGreen;
				DnColor										= Brushes.Red;
				ColorSlope									= true;
				AddPlot(Brushes.Orange, "EMA_Slope_Color");
			}

		}

		protected override void OnBarUpdate()
		{
			Value[0] = (CurrentBar == 0 ? Input[0] : Input[0] * (2.0 / (1 + Period)) + (1 - (2.0 / (1 + Period))) * Value[1]);
			
			
			if(IsRising(Value))
			{
			if(ColorSlope)
			PlotBrushes[0][0] = UpColor;
			}
			if(IsFalling(Value))
			{
			if(ColorSlope)
			PlotBrushes[0][0] = DnColor;
			}			
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Period", Description="Number of bars to include in the calculation", Order=1, GroupName="Parameters")]
		public int Period
		{ get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Up Color", Description="Color for rising condition", Order=3, GroupName="Parameters")]
		public Brush UpColor
		{ get; set; }

		[Browsable(false)]
		public string UpColorSerializable
		{
			get { return Serialize.BrushToString(UpColor); }
			set { UpColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Down Color", Description="Color for falling condition", Order=4, GroupName="Parameters")]
		public Brush DnColor
		{ get; set; }

		[Browsable(false)]
		public string DnColorSerializable
		{
			get { return Serialize.BrushToString(DnColor); }
			set { DnColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[Display(Name="Enable Slope Color?", Description="Color the plot?", Order=2, GroupName="Parameters")]
		public bool ColorSlope
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> EMA_Slope_Color
		{
			get { return Values[0]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private EMASlopeColor[] cacheEMASlopeColor;
		public EMASlopeColor EMASlopeColor(int period, Brush upColor, Brush dnColor, bool colorSlope)
		{
			return EMASlopeColor(Input, period, upColor, dnColor, colorSlope);
		}

		public EMASlopeColor EMASlopeColor(ISeries<double> input, int period, Brush upColor, Brush dnColor, bool colorSlope)
		{
			if (cacheEMASlopeColor != null)
				for (int idx = 0; idx < cacheEMASlopeColor.Length; idx++)
					if (cacheEMASlopeColor[idx] != null && cacheEMASlopeColor[idx].Period == period && cacheEMASlopeColor[idx].UpColor == upColor && cacheEMASlopeColor[idx].DnColor == dnColor && cacheEMASlopeColor[idx].ColorSlope == colorSlope && cacheEMASlopeColor[idx].EqualsInput(input))
						return cacheEMASlopeColor[idx];
			return CacheIndicator<EMASlopeColor>(new EMASlopeColor(){ Period = period, UpColor = upColor, DnColor = dnColor, ColorSlope = colorSlope }, input, ref cacheEMASlopeColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.EMASlopeColor EMASlopeColor(int period, Brush upColor, Brush dnColor, bool colorSlope)
		{
			return indicator.EMASlopeColor(Input, period, upColor, dnColor, colorSlope);
		}

		public Indicators.EMASlopeColor EMASlopeColor(ISeries<double> input , int period, Brush upColor, Brush dnColor, bool colorSlope)
		{
			return indicator.EMASlopeColor(input, period, upColor, dnColor, colorSlope);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.EMASlopeColor EMASlopeColor(int period, Brush upColor, Brush dnColor, bool colorSlope)
		{
			return indicator.EMASlopeColor(Input, period, upColor, dnColor, colorSlope);
		}

		public Indicators.EMASlopeColor EMASlopeColor(ISeries<double> input , int period, Brush upColor, Brush dnColor, bool colorSlope)
		{
			return indicator.EMASlopeColor(input, period, upColor, dnColor, colorSlope);
		}
	}
}

#endregion
