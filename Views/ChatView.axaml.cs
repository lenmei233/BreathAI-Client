using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BreathAIClient.ViewModels;
using System;

namespace BreathAIClient.Views;

public partial class ChatView : UserControl
{
    // 允许的最大行数
    private const int MaxInputLines = 11;
    // 行高估算系数（用于根据 FontSize 估算单行高度）
    private const double LineHeightFactor = 1.25;

    public ChatView()
    {
        InitializeComponent();

        // 在控件创建后订阅文本和布局变化以便重新测量
        var tb = this.FindControl<TextBox>("InputTextBox");
        if (tb != null)
        {
            // 文本变化和布局变化时都更新高度
            tb.GetObservable(TextBox.TextProperty).Subscribe(_ => UpdateInputHeight());
            tb.GetObservable(Control.BoundsProperty).Subscribe(_ => UpdateInputHeight());
        }

        // 确保附加到视觉树后做一次测量（Bounds 已准备好）
        this.AttachedToVisualTree += (_, __) => UpdateInputHeight();

        this.Loaded += (s, e) =>
        {
            // 获取当前 View 的 ViewModel，并赋值 ViewContext（当前 View 自身）
            if (this.DataContext is ChatViewModel viewModel)
            {
                viewModel.ViewContext = this; // 将 ChatView 作为上下文传递
            }
        };
    }
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);



    private void OnInputTextKeyDown(object? sender, KeyEventArgs e)
    {
        // 支持 Ctrl+Enter 发送
        if (e.Key == Key.Enter && (e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control)
        {
            var vm = DataContext as ChatViewModel;
            if (vm == null) return;

            if (vm.SendCommand is System.Windows.Input.ICommand cmd && cmd.CanExecute(null))
            {
                cmd.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void OnInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        // 委托到统一的更新方法
        UpdateInputHeight();
    }

    private void UpdateInputHeight()
    {
        var tb = this.FindControl<TextBox>("InputTextBox");
        var measure = this.FindControl<TextBlock>("MeasureTextBlock");
        var vm = DataContext as ChatViewModel;
        if (tb == null || measure == null || vm == null) return;

        if (string.IsNullOrEmpty(tb.Text))
        {
            vm.InputTextHeight = vm.InputTextMinHeight;
            return;
        }

        measure.Text = tb.Text;
        double maxWidth = tb.Bounds.Width > 0 ? tb.Bounds.Width : 200;
        double innerWidth = Math.Max(10, maxWidth - tb.Padding.Left - tb.Padding.Right);

        measure.Width = innerWidth;
        measure.Measure(new Size(innerWidth, double.MaxValue));
        double desiredHeight = measure.DesiredSize.Height + (tb.Padding.Top + tb.Padding.Bottom);

        double lineHeight = tb.FontSize * ChatView.LineHeightFactor;
        double maxHeightAllowed = lineHeight * ChatView.MaxInputLines + tb.Padding.Top + tb.Padding.Bottom;

        // 确保最终高度不超过最大值
        double finalHeight = Math.Min(desiredHeight, maxHeightAllowed);

        vm.InputTextHeight = finalHeight;
    }
}