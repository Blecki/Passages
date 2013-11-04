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
                    (context.tag as PreprocessContext).Append(Preprocess(file, ScriptEngine));
                    return null;
                },
                MISP.Arguments.Arg("filename"));

            ScriptEngine.AddFunction("preprocess", "Preprocess some text. Recursive preprocessing!",
                (context, args) =>
                {
                    var text = args[0].ToString();
                    return Preprocess(text, ScriptEngine);
                },
                MISP.Arguments.Arg("text"));

            ScriptEngine.AddFunction("write", "write to the processed output",
                (context, args) =>
                {
                    var str = args[0].ToString();
                    (context.tag as PreprocessContext).Append(MISP.Console.UnescapeString(str));
                    return null;
                },
                MISP.Arguments.Arg("string"));

            ScriptEngine.AddFunction("capture", "Capture what the second argument has written",
                (context, args) =>
                {
                    var pcontext = context.tag as PreprocessContext;
                    var oldBuilder = pcontext.builder;
                    pcontext.builder = new StringBuilder();
                    ScriptEngine.Evaluate(context, args[0], true, false);
                    var r = pcontext.builder.ToString();
                    pcontext.builder = oldBuilder;
                    return r;
                },
                MISP.Arguments.Lazy("code"));
        }

        internal class PreprocessContext
        {
            internal StringBuilder builder;

            internal void Append(String s)
            {
                builder.Append(s);
            }

        }
         
        public static String Preprocess(String text, MISP.Engine ScriptEngine)
        {
            var state = new ParseState { start = 0, end = text.Length, source = text };
            var output = new StringBuilder();
            var preprocessContext = new PreprocessContext();
            preprocessContext.builder = output;
            var preprocessGlobals = new MISP.GenericScriptObject();

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

                    var scriptContext = new MISP.Context("@globals", preprocessGlobals);
                    scriptContext.tag = preprocessContext;
                    scriptContext.limitExecutionTime = false;
                    ScriptEngine.EvaluateString(scriptContext, script.ToString(), "", false);
                    if (scriptContext.evaluationState == MISP.EvaluationState.UnwindingError)
                    {
                        Console.WriteLine("Error in preprocessing");
                        Console.WriteLine(scriptContext.errorObject["message"]);
                        Console.WriteLine("Stack trace:");
                        foreach (var item in scriptContext.errorObject["stack-trace"] as MISP.ScriptList)
                            Console.WriteLine("- " + item);
                    }
                }
            }

            return output.ToString();
        }
    }
}