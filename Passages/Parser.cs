using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Passages
{
    public class Parser
    {
        public static Passage Parse(String file)
        {
            var state = new ParseState { start = 0, end = file.Length, source = file };
            state.activePassage = new Passage();
            ParsePassageBody(state);
            return state.activePassage;
        }

        private static bool IsWhitespace(char c)
        {
            return " \t\r\n".Contains(c);
        }

        private static void DevourWhitespace(ParseState state)
        {
            while (!state.AtEnd() && " \t\r\n".Contains(state.Next())) state.Advance();
        }

        private static void DevourSpaces(ParseState state)
        {
            while (!state.AtEnd() && " \t".Contains(state.Next())) state.Advance();
        }

        private static bool ParsePassageBody(ParseState state)
        {
            state.activePassage.Leaf = false;

            //The summary must begin on the same line as the brace.
            ParseRestOfLine(out state.activePassage.summary, state);
            state.activePassage.summary = state.activePassage.summary.Trim();

            while (true)
            {
                DevourWhitespace(state);
                if (state.AtEnd())
                {
                    if (state.activePassage.parent != null)
                        throw new InvalidOperationException("Error 1003 at line " + state.currentLine);
                    else
                        return true;
                }
                if (state.Next() == '}')
                {
                    state.Advance(1);
                    return true;
                }
                else if (state.Next() == '{')
                    throw new InvalidOperationException("Error 1001 at line " + state.currentLine);
                else
                {
                    var line = "";
                    ParseRestOfLine(out line, state);
                    line = line.Trim();
                    if (String.IsNullOrEmpty(line)) throw new InvalidProgramException("Error 1002 at line " + state.currentLine);
                    var newPassage = new Passage();
                    newPassage.parent = state.activePassage;
                    newPassage.name = line;
                    DevourWhitespace(state);
                    if (!state.AtEnd() && state.Next() == '{')
                    {
                        state.Advance(1);
                        state.activePassage = newPassage;
                        if (!ParsePassageBody(state)) return false;
                        state.activePassage = newPassage.parent;
                    }
                    state.activePassage.children.Add(newPassage);
                }
            }
        }

        private static void ParseToken(out String token, ParseState state)
        {
            var t = "";
            while (!IsWhitespace(state.Next()))
            {
                t += state.Next();
                state.Advance(1);
            }
            token = t;
        }

        private static void ParseRestOfLine(out String line, ParseState state)
        {
            var t = "";
            while (!state.AtEnd() && state.Next() != '\r' && state.Next() != '\n')
            {
                t += state.Next();
                state.Advance(1);
            }
            line = t;
        }

    }
}
