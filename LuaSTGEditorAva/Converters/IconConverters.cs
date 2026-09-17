using System;
using System.Globalization;
using Avalonia.Data.Converters;
using LuaSTGEditorAva.Services;
using LuaSTGEditorSharp.EditorData;

namespace LuaSTGEditorAva.Converters
{
    public sealed class PackUriToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return IconCache.GetIcon(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class TreeNodeToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return IconCache.GetNodeIcon((value as TreeNode)?.GetType());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
