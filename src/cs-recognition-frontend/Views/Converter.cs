using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace Haru.Kei.Views;
public class FilterMaskConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		return value switch {
			Models.FilterRule.MaskValueMask => "先頭だけマスク",
			Models.FilterRule.MaskValueMaskAll => "全文マスク",
			Models.FilterRule.MaskValueReplace => "文字列置き換え",
			_ => ""
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		return DependencyProperty.UnsetValue;
	}
}

public class FilterRuleConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		return value switch {
			Models.FilterRule.RuleValueMatch => "部分一致",
			Models.FilterRule.RuleValueMatchAll => "全文一致",
			Models.FilterRule.RuleValueRegex => "正規表現",
			_ => ""
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		return DependencyProperty.UnsetValue;
	}
}