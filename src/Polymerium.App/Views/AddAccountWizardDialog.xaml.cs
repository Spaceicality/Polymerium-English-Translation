// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.Controls;
using Polymerium.App.Services;
using Polymerium.App.ViewModels;
using Polymerium.App.Views.AddAccountWizard;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Polymerium.App.Views
{
    public delegate void AddAccountWizardStateHandler(Type nextPage, bool isLast = false, Func<bool> finishAction = null);
    public sealed partial class AddAccountWizardDialog : CustomDialog
    {
        public AddAccountWizardViewModel ViewModel { get; private set; }

        private readonly AddAccountWizardStateHandler handler;
        private Func<bool> finish;
        private Type next;
        public AddAccountWizardDialog(IOverlayService overlayService)
        {
            this.InitializeComponent();
            OverlayService = overlayService;
            ViewModel = App.Current.Provider.GetRequiredService<AddAccountWizardViewModel>();
            handler = SetState;
            Root.Navigate(typeof(SelectionView), (handler, ViewModel.Source.Token));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Source.Cancel();
            Dismiss();
        }

        private void SetState(Type nextPage, bool isLast, Func<bool> finishAction)
        {
            if (nextPage != null)
            {
                NextButton.Visibility = Visibility.Visible;
                FinishButton.Visibility = Visibility.Collapsed;
                NextButton.IsEnabled = true;
            }
            else
            {
                if (isLast)
                {
                    NextButton.Visibility = Visibility.Collapsed;
                    FinishButton.Visibility = Visibility.Visible;
                }
                else
                {
                    NextButton.Visibility = Visibility.Visible;
                    FinishButton.Visibility = Visibility.Collapsed;
                    NextButton.IsEnabled = false;
                }
            }
            finish = finishAction;
            next = nextPage;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            Root.Navigate(next, (handler, ViewModel.Source.Token));
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            Root.GoBack();
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            if (finish())
                Dismiss();
        }
    }
}
