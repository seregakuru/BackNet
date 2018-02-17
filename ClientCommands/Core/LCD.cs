﻿using System;
using System.Collections.Generic;
using System.IO;

namespace ClientCommands
{
    internal class LCD : IClientCommand
    {
        public string name { get; set; } = "lcd";

        public string description { get; set; } = "Change the local current working directory";

        public string syntaxHelper { get; set; } = "lcd [newPath]";

        public bool isLocal { get; set; } = true;

        public List<string> validArguments { get; set; } = new List<string>()
        {
            "?"
        };


        public void Process(List<string> args)
        {
            if (Directory.Exists(args[0]))
            {
                Directory.SetCurrentDirectory(args[0]);
                Console.WriteLine($"lcwd => {Directory.GetCurrentDirectory()}");
            }
            else
            {
                ClientCommandsManager.networkManager.WriteLine("No such directory");
            }
        }
    }
}