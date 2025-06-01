using Livet.Messaging;
using Prism.Dialogs;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Haru.Kei.ViewModels;
internal class FilterRuleEditDialogViewModel : BindableBase, IDialogAware {
	public InteractionMessenger Messenger { get; } = new();

	public ReactiveProperty<bool> ActionMaskValue { get; } = new(initialValue: true);
	public ReactiveProperty<bool> ActionMaskAllValue { get; } = new(initialValue: false);
	public ReactiveProperty<bool> ActionReplaceValue { get; } = new(initialValue: false);
	public ReactiveProperty<bool> RuleWordValue { get; } = new(initialValue: true);
	public ReactiveProperty<bool> RuleWordAllValue { get; } = new(initialValue: false);
	public ReactiveProperty<bool> RuleRegexValue { get; } = new(initialValue: false);
	public ReactiveProperty<string> Src { get; } = new(initialValue: "");
	public ReactiveProperty<string> DstReplace { get; } = new(initialValue: "");
	public ReactiveProperty<string> DstMask { get; } = new(initialValue: "*");


	public ReactiveProperty<bool> SrcError { get; }
	public ReactiveProperty<Visibility> SrcErrorVisibility { get; }
	public ReactiveProperty<bool> DstError { get; }
	public ReactiveProperty<Visibility> DstErrorVisibility { get; }
	public ReactiveProperty<bool> OkButtonEnabled { get; }

	public ReactiveCommand OkClickCommand { get; } = new();
	public ReactiveCommand CancelClickCommand { get; } = new();

	public DialogCloseListener RequestClose { get; }

	private Models.FilterRule? input = default;

	private const string MaskValueMask = "mask";
	private const string MaskValueMaskAll = "mask-all";
	private const string MaskValueReplace = "replace";
	private const string RuleValueMatch = "match";
	private const string RuleValueMatchAll = "match-all";
	private const string RuleValueRegex = "regex";

	public FilterRuleEditDialogViewModel() {
		this.SrcError = this.Src.Select(x => !string.IsNullOrEmpty(x)).ToReactiveProperty();
		this.SrcErrorVisibility = this.SrcError.Select(x => x switch {
			true => Visibility.Hidden,
			_ => Visibility.Visible
		}).ToReactiveProperty();
		this.DstError = this.ActionReplaceValue.CombineLatest(
			this.DstReplace, this.DstMask,
			(x, _, yy) => {
				if(!x) {
					return !string.IsNullOrEmpty(yy);
				} else {
					return true;
				}
			}).ToReactiveProperty();
		this.DstErrorVisibility = this.DstError.Select(x => x switch {
			true => Visibility.Hidden,
			_ => Visibility.Visible
		}).ToReactiveProperty();
		this.OkButtonEnabled = this.SrcError
			.CombineLatest(this.DstError, (p1, p2) => p1 && p1)
			.ToReactiveProperty();

		this.OkClickCommand.Subscribe(() => {
			var ret = new DialogResult(ButtonResult.OK);
			var action = "mask";
			if(this.ActionMaskAllValue.Value) {
				action = "mask-all";
			}
			if(this.ActionReplaceValue.Value) {
				action = "replace";
			}
			var rule = "match";
			if(this.RuleWordAllValue.Value) {
				rule = "match-all";
			}
			if(this.RuleRegexValue.Value) {
				rule = "regex";
			}
			var src = this.Src.Value;
			var dst = (this.ActionMaskValue.Value || this.ActionMaskAllValue.Value) switch {
				true => this.DstMask.Value,
				_ => this.DstReplace.Value,
			};
			var rp = this.input ?? new Models.FilterRule();
			rp.Action.Value = action;
			rp.Rule.Value = rule;
			rp.Src.Value = src;
			rp.Dst.Value = dst;
			ret.Parameters.Add("result", rp);
			this.RequestClose.Invoke(ret);

			// 消す
			this.input = null;
		});
		this.CancelClickCommand.Subscribe(() => {
			this.RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
		});
	}


	public bool CanCloseDialog() {
		return true;
	}

	public void OnDialogOpened(IDialogParameters parameters) {
		if(parameters.TryGetValue("input", out object input) && input is Models.FilterRule r) {
			this.input = r;
			this.ActionMaskValue.Value
				= this.ActionMaskAllValue.Value
				= this.ActionReplaceValue.Value
				= false;
			this.RuleWordValue.Value
				= this.RuleWordAllValue.Value
				= this.RuleRegexValue.Value
				= false;
			switch(this.input.Action.Value ?? "") {
			case MaskValueMask:
				this.ActionMaskValue.Value = true;
				break;
			case MaskValueMaskAll:
				this.ActionMaskAllValue.Value = true;
				break;
			case MaskValueReplace:
				this.ActionReplaceValue.Value = true;
				break;
			}
			switch(this.input.Rule.Value ?? "") {
			case RuleValueMatch:
				this.RuleWordValue.Value = true;
				break;
			case RuleValueMatchAll:
				this.RuleWordAllValue.Value = true;
				break;
			case RuleValueRegex:
				this.RuleRegexValue.Value = true;
				break;
			}
			this.Src.Value = this.input.Src.Value ?? "";
			if(this.ActionReplaceValue.Value) {
				this.DstReplace.Value = this.input.Dst.Value ?? "";
			} else {
				this.DstMask.Value = this.input.Dst.Value ?? "*";
			}
		}
	}
	public void OnDialogClosed() {
	}

}

