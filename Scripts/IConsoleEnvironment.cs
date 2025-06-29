using UnityEngine;
using UnityEngine.UIElements;
using MoonSharp.Interpreter;
using System;
using NUnit.Framework.Internal;
using DCG.RuntimeConsole.Lua.Lexing;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using DCG.RuntimeConsole.Lua;

namespace DCG.RuntimeConsole
{
    public interface IConsoleEnvironment
    {
        /// <summary>
        /// Fired whenever output is received in this environment.
        /// </summary>
        event Action<string> OutputReceived;

        /// <summary>
        /// Execute the given input in the console.
        /// </summary>
        /// <param name="input">The input to execute.</param>
        /// <returns>True if successful, else false.</returns>
        bool Execute(string input);

        /// <summary>
        /// Resets the environment to a clean, default state.
        /// </summary>
        void Reset();
    }
}

