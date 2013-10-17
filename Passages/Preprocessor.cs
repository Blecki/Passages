using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Passages
{
    public class Preprocessor
    {
        public static void SetupScriptEngine(MISP.Engine ScriptEngine)
        {
            ScriptEngine.AddFunction("import", "Load a file and preprocess it",
                (context, args) =>
                {
                    var filename = args[0].ToString();
                    var file = System.IO.File.ReadAllText(filename);
                    return Preprocess(file, ScriptEngine);
                },
                MISP.Arguments.Arg("filename"));
        }
         
        public static String Preprocess(String text, MISP.Engine ScriptEngine)
        {
            var state = new ParseState { start = 0, end = text.Length, source = text };
            var output = new StringBuilder();

            while (!state.AtEnd())
            {
                while (!state.AtEnd() && !state.MatchNext("<<"))
                {
                    output.Append(state.Next());
                    state.Advance();
                }
                if (!state.AtEnd())
                {
                    state.Advance(2); //skip <<
                    var script = new StringBuilder();
                    while (!state.AtEnd() && !state.MatchNext(">>"))
                    {
                        script.Append(state.Next());
                        state.Advance();
                    }
                    if (!state.AtEnd()) state.Advance(2); //skip >>

                    var scriptContext = new MISP.Context();
                    scriptContext.limitExecutionTime = false;
                    var result = ScriptEngine.EvaluateString(scriptContext, script.ToString(), "", false);
                    if (result != null)
                        output.Append(result.ToString());
                }
            }

            return output.ToString();
        }
    }
}