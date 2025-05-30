using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace Haru.Kei.Views;

/// <summary>FilterのContentContainerを切り替える</summary>
public class FilterTemplateSelector : DataTemplateSelector {
	public string? SummaryDataTemplate { get; set; }
	public string? FilterDataTemplate { get; set; }

	public override DataTemplate SelectTemplate(object item, DependencyObject container) {
		var r = (item is Models.FilterItem) switch {
			true => FilterDataTemplate,
			_ => SummaryDataTemplate,
		};
		if(r == null) {
			throw new ArgumentException("!!");
		} else {
			return (DataTemplate)((FrameworkElement)container).TryFindResource(r);
		}
	}
}