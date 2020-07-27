﻿using System;
using System.Globalization;
using System.Windows.Data;
using Xarial.CadPlus.XToolbar.Services;

namespace Xarial.CadPlus.XToolbar.UI.Converters
{
    public class MacroPathToEntryPointsConverter : IValueConverter
    {
        private readonly IMacroEntryPointsExtractor m_Extractor;

        public MacroPathToEntryPointsConverter()
            : this(null)//TODO: load from services container
        {
        }

        public MacroPathToEntryPointsConverter(IMacroEntryPointsExtractor extractor)
        {
            m_Extractor = extractor;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                return m_Extractor.GetEntryPoints(value as string);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}