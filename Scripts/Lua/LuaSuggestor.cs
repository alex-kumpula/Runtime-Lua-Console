using System;
using System.Linq;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using UnityEngine;

namespace DCG.RuntimeConsole.Lua
{
    public class LuaSuggestor
    {
        public Script luaScript;

        public string GetClosestSuggestion(string code, int cursorIndex)
        {
            var (path, prefix, colonStyle) = ParseWordPath(code, cursorIndex);
            DynValue resolved = ResolveValueFromPath(this.luaScript, path);
            string suggestion = FindClosestMemberSuggestion(resolved, prefix);

            if (suggestion != null)
            {
                return suggestion;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Gets suggestions based on where the cursor is at.
        /// </summary>
        /// <param name="code">The Lua code.</param>
        /// <param name="cursorIndex">The cursor index.</param>
        /// <returns>An array of suggestions, sorted by closest to furthest.</returns>
        public string[] GetSuggestions(string code, int cursorIndex)
        {
            var (path, prefix, colonStyle) = ParseWordPath(code, cursorIndex);

            Debug.Log($"Path: {path}");

            if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(prefix))
                return Array.Empty<string>();

            DynValue resolved = ResolveValueFromPath(this.luaScript, path);

            if (resolved.Type == DataType.Table)
            {
                return resolved.Table.Keys
                    .Where(k => k.Type == DataType.String)
                    .Select(k => k.String)
                    .Where(s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(s => s.Length)
                    .ToArray();
            }

            if (resolved.Type == DataType.UserData)
            {
                var descriptor = resolved.UserData.Descriptor;

                if (descriptor is StandardUserDataDescriptor standardDesc)
                {
                    return standardDesc
                        .Members
                        .Select(m => m.Key)
                        .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(name => name.Length)
                        .ToArray();
                }
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="cursorIndex"></param>
        /// <returns></returns>
        (string path, string prefix, bool colon) ParseWordPath(string input, int cursorIndex)
        {
            int i = cursorIndex - 1;
            while (
                i >= 0 &&
                (
                    char.IsLetterOrDigit(input[i])
                    || input[i] == '_'
                    || input[i] == '.'
                    || input[i] == ':'
                )
            )
            {
                i--;
            }

            string full = input.Substring(i + 1, cursorIndex - i - 1);

            int lastDot = full.LastIndexOf('.');
            int lastColon = full.LastIndexOf(':');

            int lastSep = Math.Max(lastDot, lastColon);
            bool colonStyle = lastColon > lastDot;

            if (lastSep == -1)
            {
                return (null, full, false);
            }
                
            return (full.Substring(0, lastSep), full.Substring(lastSep + 1), colonStyle);
        }

        DynValue ResolveValueFromPath(Script script, string path)
        {
            if (string.IsNullOrEmpty(path))
                return DynValue.NewTable(script.Globals); // fallback

            string[] parts = path.Split('.', ':');

            DynValue current = DynValue.NewTable(script.Globals);

            foreach (var part in parts)
            {
                if (current.Type == DataType.Table)
                {
                    current = current.Table.Get(part);
                }
                else if (current.Type == DataType.UserData)
                {
                    var descriptor = current.UserData.Descriptor;
                    var obj = current.UserData.Object;

                    // This is the correct way to index a user data member
                    current = descriptor.Index(script, obj, DynValue.NewString(part), true);
                }
                else
                {
                    return DynValue.Nil; // Not indexable
                }
            }

            return current;
        }


        string FindClosestMemberSuggestion(DynValue value, string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return null; // Don't suggest anything if nothing typed yet

            if (value.Type == DataType.Table)
            {
                return value.Table.Keys
                    .Where(k => k.Type == DataType.String)
                    .Select(k => k.String)
                    .Where(s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(s => s.Length)
                    .FirstOrDefault();
            }

            if (value.Type == DataType.UserData)
            {
                var descriptor = value.UserData.Descriptor;

                if (descriptor is StandardUserDataDescriptor standardDesc)
                {
                    var names = standardDesc
                        .Members
                        .Select(m => m.Key)
                        .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(name => name.Length);

                    return names.FirstOrDefault();
                }
            }

            return null;
        }
    }
}

