using System;
using System.Collections.Generic;
using System.Text;
using DCG.RuntimeConsole.Lua.Lexing;
using Unity.VisualScripting;
using UnityEngine;

namespace DCG.RuntimeConsole.Lua
{
    public class LuaSyntaxHighlighter
    {
        // Map token types to hex color strings
        public Dictionary<TokenType, string> TokenColors = new Dictionary<TokenType, string>()
        {
            // Core keywords – Blue
            { TokenType.And, "#569CD6" },
            { TokenType.Break, "#569CD6" },
            { TokenType.Do, "#569CD6" },
            { TokenType.Else, "#569CD6" },
            { TokenType.ElseIf, "#569CD6" },
            { TokenType.End, "#569CD6" },
            { TokenType.False, "#569CD6" },
            { TokenType.For, "#569CD6" },
            { TokenType.Function, "#569CD6" },
            { TokenType.Lambda, "#569CD6" },
            { TokenType.Goto, "#569CD6" },
            { TokenType.If, "#569CD6" },
            { TokenType.In, "#569CD6" },
            { TokenType.Local, "#569CD6" },
            { TokenType.Nil, "#569CD6" },
            { TokenType.Not, "#569CD6" },
            { TokenType.Or, "#569CD6" },
            { TokenType.Repeat, "#569CD6" },
            { TokenType.Return, "#569CD6" },
            { TokenType.Then, "#569CD6" },
            { TokenType.True, "#569CD6" },
            { TokenType.Until, "#569CD6" },
            { TokenType.While, "#569CD6" },

            // Identifiers (names) – White
            { TokenType.Name, "#000000" },

            // Operators – Purple-ish
            { TokenType.Op_Equal, "#C586C0" },
            { TokenType.Op_Assignment, "#C586C0" },
            { TokenType.Op_LessThan, "#C586C0" },
            { TokenType.Op_LessThanEqual, "#C586C0" },
            { TokenType.Op_GreaterThanEqual, "#C586C0" },
            { TokenType.Op_GreaterThan, "#C586C0" },
            { TokenType.Op_NotEqual, "#C586C0" },
            { TokenType.Op_Concat, "#C586C0" },
            { TokenType.Op_Len, "#C586C0" },
            { TokenType.Op_Pwr, "#C586C0" },
            { TokenType.Op_Mod, "#C586C0" },
            { TokenType.Op_Div, "#C586C0" },
            { TokenType.Op_Mul, "#C586C0" },
            { TokenType.Op_MinusOrSub, "#C586C0" },
            { TokenType.Op_Add, "#C586C0" },

            // Punctuation – Gray
            { TokenType.Dot, "#C5C5C5" },
            { TokenType.Colon, "#C5C5C5" },
            { TokenType.DoubleColon, "#C5C5C5" },
            { TokenType.Comma, "#C5C5C5" },
            { TokenType.SemiColon, "#C5C5C5" },
            { TokenType.VarArgs, "#C5C5C5" },

            // Brackets – Light gray
            { TokenType.Brk_Open_Curly, "#D4D4D4" },
            { TokenType.Brk_Close_Curly, "#D4D4D4" },
            { TokenType.Brk_Open_Round, "#D4D4D4" },
            { TokenType.Brk_Close_Round, "#D4D4D4" },
            { TokenType.Brk_Open_Square, "#D4D4D4" },
            { TokenType.Brk_Close_Square, "#D4D4D4" },
            { TokenType.Brk_Open_Curly_Shared, "#D4D4D4" },

            // Strings – Orange
            { TokenType.String, "#D69D85" },
            { TokenType.String_Long, "#D69D85" },

            // Numbers – Light green
            { TokenType.Number, "#B5CEA8" },
            { TokenType.Number_Hex, "#B5CEA8" },
            { TokenType.Number_HexFloat, "#B5CEA8" },

            // Comments – Green
            { TokenType.Comment, "#6A9955" },
            { TokenType.HashBang, "#6A9955" },

            // Special/misc – Yellowish
            { TokenType.Eof, "#808080" },
            { TokenType.Invalid, "#FF0000" },
            { TokenType.Op_Dollar, "#DCDCAA" } // if used as placeholder for extension ops
        };

        public string FallbackColor = "#AAAAAA";

        public string Highlight(string code)
        {
            if (string.IsNullOrEmpty(code))
                return "";

            Lexer lexer = new Lexer(0, code, autoSkipComments: false);
            StringBuilder sb = new StringBuilder();

            // Splits the given code into lines,
            // on either Windows or Unix line-endings.
            string[] lines = code.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

            int cursorLine = 1;
            int cursorCol = 0;

            try
            {
                // Advance to first token
                lexer.Next();

                // Iterate over each token until we reach Eof (end of file)
                while (lexer.Current.Type != TokenType.Eof)
                {
                    Token currentToken = lexer.Current;

                    string tokenText = GetStringByLineAndCol(
                        lines: lines,
                        lineFrom: cursorLine,
                        lineTo: currentToken.ToLine,
                        colFrom: cursorCol,
                        colTo: currentToken.ToCol
                    );
                    string tokenColor = this.GetColor(currentToken.Type);

                    sb.Append($"<color={tokenColor}>{tokenText}</color>");

                    // End by moving the lexer to the next token
                    cursorLine = currentToken.ToLine;
                    cursorCol = currentToken.ToCol;
                    lexer.Next();
                }
            }
            catch (SyntaxErrorException)
            {
                // Lexer broke on invalid/unterminated input
            }

            // Add any trailing text after the final token
            int cursorLineIndex = cursorLine - 1;
            while (cursorLineIndex < lines.Length)
            {
                if (cursorCol < lines[cursorLineIndex].Length)
                    sb.Append(Escape(lines[cursorLineIndex].Substring(cursorCol)));

                if (cursorLineIndex < lines.Length - 1)
                    sb.Append("\n");

                cursorLineIndex++;
                cursorCol = 0;
            }

            return sb.ToString();
        }

        public static string GetStringByLineAndCol(string[] lines, int lineFrom, int lineTo, int colFrom, int colTo)
        {
            if (lines == null || lines.Length == 0)
                return string.Empty;

            // Since we assume lines in Lua actually start at 1, not 0
            int lineIndexFrom = lineFrom - 1;
            int lineIndexTo = lineTo - 1;

            if (lineIndexFrom < 0 || lineIndexFrom >= lines.Length || lineIndexTo < 0 || lineIndexTo >= lines.Length)
                throw new ArgumentOutOfRangeException("Line indices are out of range.");

            var sb = new StringBuilder();

            if (lineIndexFrom == lineIndexTo)
            {
                // Single-line case
                string line = lines[lineIndexFrom];
                int length = Math.Min(colTo - colFrom, line.Length - colFrom);
                return line.Substring(colFrom, length);
            }

            // Multi-line case
            // First line: from colFrom to end
            sb.AppendLine(lines[lineIndexFrom].Substring(colFrom));

            // Middle lines: full lines
            for (int i = lineIndexFrom + 1; i < lineIndexTo; i++)
            {
                sb.AppendLine(lines[i]);
            }

            // Last line: from 0 to colTo
            sb.Append(lines[lineIndexTo].Substring(0, colTo));

            return sb.ToString();
        }

        private static string Escape(string text)
        {
            return text.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private string GetColor(TokenType type)
        {
            return this.TokenColors.TryGetValue(type, out var color) ? color : "#FFFFFF";
        }
    }
}

