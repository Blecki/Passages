using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Passages
{
    class Program
    {
        static internal String Version = "0.1";

        static void Main(string[] args)
        {
            var options = new SwitchOptions();
            if (!CommandLine.ParseArguments(args, options))
                return;

            try
            {
                if (options.mode == "pre")
                {
                    System.IO.FileStream destination = null;// System.IO.File.Open(options.outFile, System.IO.FileMode.Create);
                    System.IO.StreamWriter writer = null;// new System.IO.StreamWriter(destination);

                    var scriptEngine = new MISP.Engine();
                    Preprocessor.SetupScriptEngine(scriptEngine);

                    scriptEngine.AddGlobalVariable("options", (c) => { return options; });


                    scriptEngine.AddFunction("retarget", "redirect the output of the preprocessor",
                        (context, cargs) =>
                        {
                            var newTarget = cargs[0].ToString();

                            //Grab existing buffer
                            var bufferBuilder = context.tag as Preprocessor.PreprocessContext;
                            var buffer = bufferBuilder.builder.ToString();

                            if (writer == null && !String.IsNullOrEmpty(buffer))
                            {
                                destination = System.IO.File.Open(options.outFile, System.IO.FileMode.Create);
                                writer = new System.IO.StreamWriter(destination);
                            }

                            if (writer != null)
                            {
                                if (!String.IsNullOrEmpty(buffer))
                                    writer.Write(buffer);

                                writer.Flush();
                                destination.Flush();
                                destination.Close();
                            }

                            destination = System.IO.File.Open(newTarget, System.IO.FileMode.Create);
                            writer = new System.IO.StreamWriter(destination);

                            return null;
                        }, MISP.Arguments.Arg("filename"));


                    var source = System.IO.File.ReadAllText(options.inFile);
                    var processed = Preprocessor.Preprocess(source, scriptEngine);

                    if (writer == null)
                    {
                        destination = System.IO.File.Open(options.outFile, System.IO.FileMode.Create);
                        writer = new System.IO.StreamWriter(destination);
                    }
                    if (!String.IsNullOrEmpty(processed))
                        writer.Write(processed);

                    writer.Flush();
                    destination.Flush();
                    destination.Close();
                }
                else if (options.mode == "extract-prose")
                {
                    var destination = System.IO.File.Open(options.outFile, System.IO.FileMode.Create);
                    var writer = new System.IO.StreamWriter(destination);

                    var scriptEngine = new MISP.Engine();
                    Preprocessor.SetupScriptEngine(scriptEngine);

                    scriptEngine.AddGlobalVariable("options", (c) => { return options; });

                    var source = System.IO.File.ReadAllText(options.inFile);
                    var processed = Preprocessor.Preprocess(source, scriptEngine);

                    foreach (var item in new ExtractProse(Parser.Parse(processed)))
                        writer.WriteLine(item);

                    writer.Flush();
                    destination.Flush();
                    destination.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured.");
                Console.WriteLine(e.Message);
            }
        }

    }
}
