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
    public class ConsoleEnvironmentLua : IConsoleEnvironment
    {
        private Script luaScript;

        public ConsoleEnvironmentLua()
        {
            // Initialize Lua
            this.luaScript = new Script(CoreModules.Preset_SoftSandbox);

            // Add globals to the Lua environment
            this.luaScript.Globals["log"] = (System.Action<string>)((msg) => Debug.Log("[Lua] " + msg));

            this.luaScript.Globals["hello"] = (System.Action<string>)(x => Debug.Log("Hello, " + x));
        }

        public Script LuaScript
        {
            get { return this.luaScript; }
        }

        public event Action<string> OutputReceived;

        public bool Execute(string input)
        {
            // Testing
            foreach (DynValue key in this.luaScript.Globals.Keys)
            {
                Debug.Log(key.ToString());
            }


            // Extract tokens with lexer
            Lexer lexer = new Lexer(1, input, false);

            List<Token> tokens = new();

            while (lexer.Current.Type != TokenType.Eof)
            {
                tokens.Add(lexer.Current);

                Debug.Log(lexer.Current.Type.ToString());
                Debug.Log(lexer.Current.Text.ToString());
                Debug.Log($"Line: {lexer.Current.FromLine} ... Col: {lexer.Current.FromCol} - {lexer.Current.ToCol}");

                lexer.Next();
            }

            // LuaSyntaxHighlighter syntaxHighlighter = new();
            // Debug.Log(syntaxHighlighter.Highlight(input));


            // Execute input
            try
            {
                DynValue result = this.luaScript.DoString(input);

                if (result.Type != DataType.Void && result.Type != DataType.Nil)
                {
                    this.OutputReceived?.Invoke(result.ToString());
                }

                return true;
            }
            catch (ScriptRuntimeException ex)
            {
                this.OutputReceived?.Invoke($"Error: {ex.DecoratedMessage}");
                return false;
            }
        }

        public void Reset()
        {
            this.luaScript = new Script(CoreModules.Preset_SoftSandbox);
        }
    }
}

