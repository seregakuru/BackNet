﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClientCommands
{
    internal class Cmd : ICommand
    {
        public string name { get; } = "cmd";

        public string description { get; } = "Opens a Windows command prompt to interact with, use 'exit' to exit the cmd prompt and return";

        public string syntaxHelper { get; } = "cmd";

        public bool isLocal { get; } = false;

        public List<string> validArguments { get; } = null;


        public bool PreProcess(List<string> args)
        {
            throw new NotImplementedException();
        }

        public void Process(List<string> args)
        {
            var ts = new CancellationTokenSource();
            var cancelToken = ts.Token;
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var receivedData = ClientCommandsManager.networkManager.ReadLine();

                    // Main thread exited and notified this task to stop
                    if (cancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // Normal output (text)
                    // Check if the line isn't the one representing the path in the cmd
                    if (receivedData.Length > 0 && !(receivedData[receivedData.Length - 1] == '>' && receivedData.Contains(@":\")))
                    {
                        // Add line return
                        Console.WriteLine(receivedData);
                    }
                    else
                    {
                        // Display cmd path without line return
                        Console.Write(receivedData);
                    }
                }
            }, cancelToken);


            while (true)
            {
                var userInput = Console.ReadLine();

                if (userInput == "cls" || userInput == "clear")
                {
                    userInput = "";
                    Console.Clear();
                }

                ClientCommandsManager.networkManager.WriteLine(userInput);
                if (userInput == "exit")
                {
                    // Cancel the listening task
                    ts.Cancel();
                    break;
                }
            }
        }
    }
}
