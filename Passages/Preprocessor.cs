using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Passages
{
    public class Preprocessor
    {
        public static String UnescapeString(String s)
        {
            var place = 0;
            var r = "";
            while (place < s.Length)
            {
                if (s[place] == '\\')
                {
                    if (place < s.Length - 1)
                    {
                        if (s[place + 1] == 'n')
                            r += '\n';
                        else if (s[place + 1] == 't')
                            r += '\t';
                        else if (s[place + 1] == 'r')
                            r += '\r';
                        else r += s[place + 1];
                    }
                    place += 2;
                }
                else
                {
                    r += s[place];
                    ++place;
                }
            }
            return r;
        }

        public static void SetupScriptEngine(MISP.Environment ScriptEngine)
        {
            ScriptEngine.AddNativeFunction("import",
                (context, args) =>
                {
                    var filename = args[0].ToString();
                    var file = System.IO.File.ReadAllText(filename);
                    (context.Tag as PreprocessContext).Append(Preprocess(file, ScriptEngine));
                    return null;
                });

            ScriptEngine.AddNativeFunction("preprocess", 
                (context, args) =>
                {
                    var text = args[0].ToString();
                    return Preprocess(text, ScriptEngine);
                });

            ScriptEngine.AddNativeFunction("write",
                (context, args) =>
                {
                    var str = args[0].ToString();
                    (context.Tag as PreprocessContext).Append(UnescapeString(str));
                    return null;
                });

            ScriptEngine.AddNativeFunction("capture",
                (context, args) =>
                {
                    var pcontext = context.Tag as PreprocessContext;
                    var oldBuilder = pcontext.builder;
                    pcontext.builder = new StringBuilder();
                    ScriptEngine.RunScript(args[0].ToString());
                    var r = pcontext.builder.ToString();
                    pcontext.builder = oldBuilder;
                    return r;
                });
        }

        internal class PreprocessContext
        {
            internal StringBuilder builder;

            internal void Append(String s)
            {
                builder.Append(s);
            }

        }
         
        public static String Preprocess(String text, MISP.Environment ScriptEngine)
        {
            var state = new ParseState { start = 0, end = text.Length, source = text };
            var output = new StringBuilder();
            var preprocessContext = new PreprocessContext();
            preprocessContext.builder = output;
            var preprocessGlobals = new MISP.ScriptObject();

            while (!state.AtEnd())
            {
                while (!state.AtEnd() && !state.MatchNext("<<"))
                {
                    if (state.MatchNext("\\<<")) //Skip escaped open brackets
                    {
                        output.Append("<<");
                        state.Advance(3);
                    }
                    else
                    {
                        output.Append(state.Next());
                        state.Advance();
                    }
                }
                if (!state.AtEnd())
                {
                    state.Advance(2); //skip <<
                    var script = new StringBuilder();
                    while (!state.AtEnd() && !state.MatchNext(">>"))
                    {
                        if (state.MatchNext("\\>>"))
                        {
                            script.Append(">>");
                            state.Advance(3);
                        }
                        else
                        {
                            script.Append(state.Next());
                            state.Advance();
                        }
                    }
                    if (!state.AtEnd()) state.Advance(2); //skip >>

                    var scriptContext = ScriptEngine.CompileScript(script.ToString());
                    scriptContext.Tag = preprocessContext;
                    ScriptEngine.RunScript(scriptContext);
                    if (scriptContext.ExecutionState == MISP.ExecutionState.Error)
                    {
                        Console.WriteLine("Error in preprocessing");
                        Console.WriteLine(scriptContext.ErrorMessage);
                    }
                }
            }

            return output.ToString();
        }
    }
}