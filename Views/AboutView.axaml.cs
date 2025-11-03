using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;

namespace BreathAIClient.Views;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
    }


    private void TextBlock_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/lenmei233/BreathAI-Client") { UseShellExecute = true });
    }

    private void TextBlock_PointerPressed_1(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://chat.breathai.top") { UseShellExecute = true });
    }
}