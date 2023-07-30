﻿using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Downloader;
using System;
using System.Threading.Tasks;

namespace CmlLibCoreSample
{
    class Program
    {
        public static async Task Main()
        {
            var p = new Program();

            // Login
            MSession session; // login session

            // There are two login methods, one is using mojang email and password, and the other is using only username
            // Choose one which you want.
            //session = await p.PremiumLogin(); // Login by mojang email and password
            session = p.OfflineLogin(); // Login by username

            // log login session information
            Console.WriteLine("Success to login : {0} / {1} / {2}", session.Username, session.UUID, session.AccessToken);

            // Launch
            await p.Start(session);
            //p.StartAsync(session).GetAwaiter().GetResult();
        }

        async Task<MSession> PremiumLogin()
        {
            var login = new MLogin();

            // TryAutoLogin() reads the login cache file and check validation.
            // If the cached session is invalid, it refreshes the session automatically.
            // Refreshing the session doesn't always succeed, so you have to handle this.
            Console.WriteLine("Attempting to automatically log in.");
            var response = await login.TryAutoLogin();

            if (!response.IsSuccess) // if cached session is invalid and failed to refresh token
            {
                Console.WriteLine("Auto login failed: {0}", response.Result.ToString());

                Console.WriteLine("Input your Mojang email: ");
                var email = Console.ReadLine();
                Console.WriteLine("Input your Mojang password: ");
                var pw = Console.ReadLine();

                response = await login.Authenticate(email, pw);

                if (!response.IsSuccess)
                {
                    // session.Message contains a detailed error message. It can be null or an empty string.
                    Console.WriteLine("failed to login. {0} : {1}", response.Result.ToString(), response.ErrorMessage);
                    Console.ReadLine();
                    Environment.Exit(0);
                    return null;
                }
            }

            return response.Session;
        }

        MSession OfflineLogin()
        {
            // Create fake session by username
            return MSession.GetOfflineSession("tester123");
        }

        async Task Start(MSession session)
        {
            // Initializing Launcher

            // Set minecraft home directory
            // MinecraftPath.GetOSDefaultPath() return default minecraft BasePath of current OS.
            // https://github.com/AlphaBs/CmlLib.Core/blob/master/CmlLib/Core/MinecraftPath.cs

            // You can set this path to what you want like this :
            //var path = "./testdir";
            var path = MinecraftPath.GetOSDefaultPath();
            var game = new MinecraftPath(path);

            // Create CMLauncher instance
            var launcher = new CMLauncher(game);

            // if you want to download with parallel downloader, add below code :
            System.Net.ServicePointManager.DefaultConnectionLimit = 256;

            // for offline mode
            //launcher.VersionLoader = new LocalVersionLoader(launcher.MinecraftPath);
            //launcher.FileDownloader = null;
            
            launcher.ProgressChanged += Downloader_ChangeProgress;
            launcher.FileChanged += Downloader_ChangeFile;

            Console.WriteLine($"Initialized in {launcher.MinecraftPath.BasePath}");

            // Get all installed profiles and load all profiles from mojang server
            var versions = await launcher.GetAllVersionsAsync(); 

            foreach (var item in versions) // Display all profiles 
            {
                // You can filter snapshots and old versions to add if statement : 
                // if (item.MType == MProfileType.Custom || item.MType == MProfileType.Release)
                Console.WriteLine(item.Type + " " + item.Name);
            }

            var launchOption = new MLaunchOption
            {
                MaximumRamMb = 1024,
                Session = session,

                //ScreenWidth = 1600,
                //ScreenHeight = 900,
                //ServerIp = "mc.hypixel.net",
                //MinimumRamMb = 102,
                //FullScreen = true,

                // More options:
                // https://github.com/AlphaBs/CmlLib.Core/wiki/MLaunchOption
            };

            // download essential files (ex: vanilla libraries) and create game process.

            // var process = await launcher.CreateProcessAsync("1.15.2", launchOption); // vanilla
            // var process = await launcher.CreateProcessAsync("1.12.2-forge1.12.2-14.23.5.2838", launchOption); // forge
            // var process = await launcher.CreateProcessAsync("1.12.2-LiteLoader1.12.2"); // liteloader
            // var process = await launcher.CreateProcessAsync("fabric-loader-0.11.3-1.16.5") // fabric-loader

            Console.WriteLine("input version (example: 1.12.2) : ");
            var versionName = Console.ReadLine();
            //var versionName = "1.18.2";
            var process = await launcher.CreateProcessAsync(versionName, launchOption);

            //var process = launcher.CreateProcess("1.16.2", "33.0.5", launchOption);
            Console.WriteLine(process.StartInfo.FileName);
            Console.WriteLine(process.StartInfo.Arguments);

            // Below codes are print game logs in Console.
            var processUtil = new CmlLib.Utils.ProcessUtil(process);
            processUtil.OutputReceived += (s, e) => Console.WriteLine(e);
            processUtil.StartWithEvents();
            await process.WaitForExitAsync();

            // or just start it without print logs
            // process.Start();

            Console.ReadLine();
        }

        #region QuickStart

        // this code is from README.md

        async Task QuickStart()
        {
            //var path = new MinecraftPath("game_directory_path");
            var path = new MinecraftPath(); // use default directory

            var launcher = new CMLauncher(path);
            launcher.FileChanged += (e) =>
            {
                Console.WriteLine("[{0}] {1} - {2}/{3}", e.FileKind.ToString(), e.FileName, e.ProgressedFileCount, e.TotalFileCount);
            };
            launcher.ProgressChanged += (s, e) =>
            {
                Console.WriteLine("{0}%", e.ProgressPercentage);
            };

            var versions = await launcher.GetAllVersionsAsync();
            foreach (var item in versions)
            {
                Console.WriteLine(item.Name);
            }

            var launchOption = new MLaunchOption
            {
                MaximumRamMb = 1024,
                Session = MSession.GetOfflineSession("hello"), // Login Session. ex) Session = MSession.GetOfflineSession("hello")

                //ScreenWidth = 1600,
                //ScreenHeigth = 900,
                //ServerIp = "mc.hypixel.net"
            };

            // launch vanila
            var process = await launcher.CreateProcessAsync("1.15.2", launchOption);

            process.Start();
        }

        #endregion

        // Event Handling

        // The code below has some tricks to display logs prettier.
        // You can also use a simpler event handler

        #region Pretty event handler

        private void Downloader_ChangeProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            var top = Console.CursorTop;
            Console.SetCursorPosition(0, top);
            // e.ProgressPercentage: 0~100
            Console.Write($"{e.ProgressPercentage}%  ");
            Console.SetCursorPosition(0, top);
        }

        private void Downloader_ChangeFile(DownloadFileChangedEventArgs e)
        {
            // More information about DownloadFileChangedEventArgs
            // https://github.com/AlphaBs/CmlLib.Core/wiki/Handling-Events#downloadfilechangedeventargs

            Console.WriteLine("[{0}] ({2}/{3}) {1}   ", e.FileKind.ToString(), e.FileName, e.ProgressedFileCount, e.TotalFileCount);
        }

        #endregion

        #region Simple event handler
        //private void Downloader_ChangeProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        //{
        //    Console.WriteLine("{0}%", e.ProgressPercentage);
        //}

        //private void Downloader_ChangeFile(DownloadFileChangedEventArgs e)
        //{
        //    Console.WriteLine("[{0}] {1} - {2}/{3}", e.FileKind.ToString(), e.FileName, e.ProgressedFileCount, e.TotalFileCount);
        //}
        #endregion
    }
}

