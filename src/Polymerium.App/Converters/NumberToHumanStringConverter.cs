﻿using System;
using Humanizer;
using Microsoft.UI.Xaml.Data;

namespace Polymerium.App.Converters;

public class NumberToHumanStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            long it => ((int)it).ToMetric(MetricNumeralFormats.WithSpace, 2),
            int it => it.ToMetric(),
            double it => it.ToMetric(),
            float it => ((double)it).ToMetric(),
            _ => value
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}