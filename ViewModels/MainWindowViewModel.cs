using BreathAIClient.Models;
using BreathAIClient.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BreathAIClient.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
    private ChatViewModel _chat;
    private SettingsViewModel _settings;
    private AboutViewModel _about;

        public ChatViewModel Chat
    {
        get => _chat;
        set => this.RaiseAndSetIfChanged(ref _chat, value);
    }

    public SettingsViewModel Settings
    {
        get => _settings;
        set => this.RaiseAndSetIfChanged(ref _settings, value);
    }
        public AboutViewModel About
            {
            get => _about;
            set => this.RaiseAndSetIfChanged(ref _about, value);
        }
        public MainWindowViewModel(ChatViewModel chatVm, SettingsViewModel settingsVm , AboutViewModel aboutVm)
    {
        Chat = chatVm;
        Settings = settingsVm;
            About = aboutVm;
        }
    }
}
