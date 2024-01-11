﻿using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace SimpleModpackDownloader.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (sender, args) => SystemThemeWatcher.Watch(this);
    }
}
