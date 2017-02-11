using System;
using System.Reflection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace BluetoothChat.Converters
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            var enumToBool = new EnumToBooleanConverter();
            var convertedValue = enumToBool.Convert(value, typeof(bool), parameter, culture);
            if (convertedValue == DependencyProperty.UnsetValue)
            {
                return Visibility.Collapsed;
            }
            bool convertedBool = (bool)convertedValue;

            var boolToVis = new BooleanToVisibilityConverter();
            return boolToVis.Convert(convertedBool, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (targetType != typeof(bool) && targetType != (typeof(bool?)))
            {
                throw new InvalidOperationException("The target must be a bool");
            }

            string parameterString = parameter as string;
            if (string.IsNullOrEmpty(parameterString))
            {
                return DependencyProperty.UnsetValue;
            }

            if (value == null || !IsEnum(value.GetType()))
            {
                return DependencyProperty.UnsetValue;
            }

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return value.Equals(parameterValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            string parameterString = parameter as string;
            if (string.IsNullOrEmpty(parameterString))
            {
                return DependencyProperty.UnsetValue;
            }

            return Enum.Parse(targetType, parameterString);
        }

        /// <summary>
        /// Gets whether a type is an enum or nullable enum.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsEnum(Type t)
        {
            var underlyingType = (Nullable.GetUnderlyingType(t) ?? t);
            return underlyingType.GetTypeInfo().IsEnum;
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        #region PUBLIC METHODS

        // -------------------------------------------------------------------------------------------------------

        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (targetType != typeof(Visibility))
            {
                throw new InvalidOperationException("The target must be a Visibility");
            }

            if (!(value is bool) || value == DependencyProperty.UnsetValue)
            {
                return Visibility.Collapsed;
            }

            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotSupportedException();
        }

        #endregion PUBLIC METHODS
    }
}