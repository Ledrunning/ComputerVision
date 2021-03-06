﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Data;
using System.Windows.Input;
using CVCapturePanel.Helpers;
using CVCapturePanel.Model;
using CVCapturePanel.Service;
using Emgu.CV;
using Emgu.CV.Structure;

namespace CVCapturePanel.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        public delegate void ImageWithDetectionChangedEventHandler(object sender, Image<Bgr, byte> image);

        private readonly IDialogService dialog = new DialogService();
        private readonly IList<VideoSource> sourceList = new List<VideoSource>();
        private readonly WebCamService webCamService = new WebCamService();
        private string buttonContent = "Start";
        private Bitmap frame;

        private bool isStreaming;
        private string selectedVideoSource;

        private VideoPlayingService videoPlayingService;

        /// <summary>
        ///     .ctor
        /// </summary>
        public MainViewModel(Action methodAction)
        {
            InitializeServices();
            InitializeCommands();
            FillComboBox();
            CloseAction = methodAction;
        }

        /// <summary>
        ///     Property for webCam service
        /// </summary>
        public ICommand ToggleWebServiceCommand { get; set; }

        public ICommand ToogleOpenVideoCommand { get; set; }

        public ICommand ToogleCloseAppCommand { get; set; }

        public CollectionView Video { get; private set; }
        public Action CloseAction { get; set; }
        public string OpenSource { get; } = "Open video";
        public string CloseSource { get; } = "Close video";
        public string Exit { get; } = "Exit";

        public string SelectSource { get; set; } = "Select source";

        public string ButtonContent
        {
            get => buttonContent;
            set
            {
                buttonContent = value;
                OnPropertyChanged();
            }
        }

        public string SelectedVideoSource
        {
            get => selectedVideoSource;
            set
            {
                if (selectedVideoSource == value)
                {
                    return;
                }

                selectedVideoSource = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Start webCam service button toogle
        /// </summary>
        public bool IsStreaming
        {
            get => isStreaming;
            set
            {
                isStreaming = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Property for View Image component notification
        /// </summary>
        public Bitmap Frame
        {
            get => frame;

            set => SetField(ref frame, value);
        }

        public event ImageWithDetectionChangedEventHandler ImageWithDetectionChanged;

        private void InitializeWebCamService()
        {
            webCamService.ImageChanged += WebCamServiceOnImageChanged;
        }

        private void RaiseImageWithDetectionChangedEvent(Image<Bgr, byte> image)
        {
            ImageWithDetectionChanged?.Invoke(this, image);
        }

        private void WebCamServiceOnImageChanged(object sender, Image<Bgr, byte> image)
        {
            //RaiseImageWithDetectionChangedEvent(image);
            Frame = image.Bitmap;
        }

        private void FillComboBox()
        {
            sourceList.Add(new VideoSource("Video"));
            sourceList.Add(new VideoSource("Camera capture"));
            Video = new CollectionView(sourceList);
        }

        private void InitializeServices()
        {
            videoPlayingService = new VideoPlayingService();
            videoPlayingService.VideoFramesChangeEvent += VideoPlayingServiceVideoFramesChangeEvent;
            InitializeWebCamService();
        }

        private void VideoPlayingServiceVideoFramesChangeEvent(object sender, Mat frame)
        {
            Frame = frame.Bitmap;
        }

        /// <summary>
        ///     Initialize all commands from main view model
        /// </summary>
        private void InitializeCommands()
        {
            ToggleWebServiceCommand = new RelayCommand(ToggleWebServiceExecute);
            ToogleOpenVideoCommand = new RelayCommand(ToogleOpenVideo);
            ToogleCloseAppCommand = new RelayCommand(ToogleCloseApp);
        }

        /// <summary>
        ///     Service From WebCamService
        /// </summary>
        private void ToggleWebServiceExecute()
        {
            if (SelectedVideoSource == sourceList[1].Name)
            {
                if (!webCamService.IsRunning)
                {
                    IsStreaming = true;
                    ButtonContent = "Stop";
                    webCamService.RunServiceAsync();
                }
                else
                {
                    IsStreaming = false;
                    ButtonContent = "Start";
                    webCamService.CancelServiceAsync();
                    ClearFrame();
                }
            }
            else if (SelectedVideoSource == sourceList[0].Name)
            {
                if (!videoPlayingService.IsPlaying)
                {
                    IsStreaming = true;
                    ButtonContent = "Stop";
                    ToogleOpenVideo();
                }
                else
                {
                    IsStreaming = false;
                    ButtonContent = "Start";
                    videoPlayingService.VideoFramesChangeEvent -= VideoPlayingServiceVideoFramesChangeEvent;
                    videoPlayingService.Dispose();
                    videoPlayingService.StopPlaying();

                    ClearFrame();
                }
            }
        }

        private void ClearFrame()
        {
            if (Frame != null)
            {
                Frame = null;
            }
        }

        private void ToogleOpenVideo()
        {
            if (dialog.OpenFileDialog())
            {
                videoPlayingService.PlayVideo(dialog.FilePath);
            }
        }

        private void ToogleCloseApp()
        {
            CloseAction?.Invoke();
            if (webCamService != null)
            {
                webCamService.Dispose();
                videoPlayingService.Dispose();
            }
        }
    }
}