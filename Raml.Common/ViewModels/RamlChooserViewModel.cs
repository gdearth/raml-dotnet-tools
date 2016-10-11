﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;
using Microsoft.Win32;

namespace Raml.Common.ViewModels
{
    public class RamlChooserViewModel : Screen
    {
        private const string RamlFileExtension = ".raml";
        // action to execute when clicking Ok button (add RAML Reference, Scaffold Web Api, etc.)
        private Action<RamlChooserActionParams> action;
        private string exchangeUrl;
        public string RamlTempFilePath { get; private set; }
        public string RamlOriginalSource { get; set; }

        public IServiceProvider ServiceProvider { get; set; }

        private bool isContractUseCase;
        private int height;
        private string url;
        private string title;
        private Visibility progressBarVisibility;
        private bool isNewRamlOption;
        private bool existingRamlOption;

        private bool IsContractUseCase
        {
            get { return isContractUseCase; }
            set
            {
                if (value.Equals(isContractUseCase)) return;
                isContractUseCase = value;
                NotifyOfPropertyChange("ContractUseCaseVisibility");
            }
        }

        public Visibility ContractUseCaseVisibility
        {
            get
            {
                return IsContractUseCase ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void Load(IServiceProvider serviceProvider, Action<RamlChooserActionParams> action, string title, bool isContractUseCase, string exchangeUrl)
        {
            this.action = action;
            this.exchangeUrl = exchangeUrl; // "https://qa.anypoint.mulesoft.com/exchange/#!/?types=api"; // testing URL
            ServiceProvider = serviceProvider;
            DisplayName = title;
            IsContractUseCase = isContractUseCase;
            Height = isContractUseCase ? 750 : 375;
            StopProgress();
        }

        public int Height
        {
            get { return height; }
            set
            {
                if (value == height) return;
                height = value;
                NotifyOfPropertyChange(() => Height);
            }
        }

        public bool CanAddNewContract
        {
            get { return !string.IsNullOrWhiteSpace(Title); }
        }

        public async void AddExistingRamlFromDisk()
        {
            SelectExistingRamlOption();
            FileDialog fd = new OpenFileDialog();
            fd.DefaultExt = ".raml;*.rml";
            fd.Filter = "RAML files |*.raml;*.rml";

            var opened = fd.ShowDialog();

            if (opened != true)
            {
                return;
            }

            RamlTempFilePath = fd.FileName;
            RamlOriginalSource = fd.FileName;

            var title = Path.GetFileName(fd.FileName);

            
            var preview = new RamlPreview(ServiceProvider, action, RamlTempFilePath, RamlOriginalSource, title, isContractUseCase);
            
            StartProgress();
            await preview.FromFile();
            StopProgress();

            var dialogResult = preview.ShowDialog();
            if(dialogResult == true)
                TryClose();
        }

        private void StartProgress()
        {
            ProgressBarVisibility = Visibility.Visible;
            Mouse.OverrideCursor = Cursors.Wait;
        }

        public Visibility ProgressBarVisibility
        {
            get { return progressBarVisibility; }
            set
            {
                if (value == progressBarVisibility) return;
                progressBarVisibility = value;
                NotifyOfPropertyChange(() => ProgressBarVisibility);
            }
        }

        private void StopProgress()
        {
            ProgressBarVisibility = Visibility.Hidden;
            Mouse.OverrideCursor = null;
        }

        public async void AddExistingRamlFromExchange()
        {
            SelectExistingRamlOption();
            var rmlLibrary = new RAMLLibraryBrowser(exchangeUrl);
            var selectedRamlFile = rmlLibrary.ShowDialog();

            if (selectedRamlFile.HasValue && selectedRamlFile.Value)
            {
                var url = rmlLibrary.RAMLFileUrl;

                Url = url;

                var preview = new RamlPreview(ServiceProvider, action, RamlTempFilePath, url, "title", isContractUseCase);
                
                StartProgress();
                await preview.FromURL();
                StopProgress();

                var dialogResult = preview.ShowDialog();
                if (dialogResult == true)
                    TryClose();
            }
        }

        public string Url
        {
            get { return url; }
            set
            {
                if (value == url) return;
                url = value;
                NotifyOfPropertyChange(() => Url);
                NotifyOfPropertyChange(() => CanAddExistingRamlFromUrl);
            }
        }


        public async void AddExistingRamlFromUrl()
        {
            SelectExistingRamlOption();
            var preview = new RamlPreview(ServiceProvider, action, RamlTempFilePath, Url, "title", isContractUseCase);
            
            StartProgress();
            await preview.FromURL();
            StopProgress();

            var dialogResult = preview.ShowDialog();
            if(dialogResult == true)
                TryClose();
        }

        public bool IsNewRamlOption
        {
            get { return isNewRamlOption; }
            set
            {
                if (value == isNewRamlOption) return;
                isNewRamlOption = value;
                NotifyOfPropertyChange(() => IsNewRamlOption);
            }
        }

        public bool CanAddExistingRamlFromUrl
        {
            get { return !string.IsNullOrWhiteSpace(Url); }
        }

        public bool ExistingRamlOption
        {
            get { return existingRamlOption; }
            set
            {
                if (value == existingRamlOption) return;
                existingRamlOption = value;
                NotifyOfPropertyChange(() => ExistingRamlOption);
            }
        }

        public void Url_Changed()
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            SelectExistingRamlOption();
        }

        public void Title_Changed()
        {
            if (string.IsNullOrWhiteSpace(title)) 
                return;

            SelectNewRamlOption();
            NewRamlFilename = NetNamingMapper.RemoveIndalidChars(Title) + RamlFileExtension;
            NewRamlNamespace = GetNamespace(NewRamlFilename);
        }

        public string Title
        {
            get { return title; }
            set
            {
                if (value == title) return;
                title = value;
                NotifyOfPropertyChange(() => Title);
                NotifyOfPropertyChange(() => CanAddNewContract);
            }
        }

        public string NewRamlNamespace { get; set; }

        public string NewRamlFilename { get; set; }

        private string GetNamespace(string fileName)
        {
            return VisualStudioAutomationHelper.GetDefaultNamespace(ServiceProvider) + "." +
                     NetNamingMapper.GetObjectName(Path.GetFileNameWithoutExtension(fileName));
        }

        private void SelectExistingRamlOption()
        {
            ExistingRamlOption = true;
            IsNewRamlOption = false;
        }

        private void SelectNewRamlOption()
        {
            IsNewRamlOption = true;
            ExistingRamlOption = false;
        }

        public void Cancel()
        {
            TryClose();
        }

        public void AddNewContract()
        {
            var preview = new RamlPreview(ServiceProvider, action, Title);
            preview.NewContract();
            preview.ShowDialog();
            TryClose();
        }
    }
}