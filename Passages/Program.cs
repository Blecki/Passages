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
                var scriptEngine = new MISP.Engine();
                Preprocessor.SetupScriptEngine(scriptEngine);
                scriptEngine.AddGlobalVariable("options", (c) => { return options; });
                var source = System.IO.File.ReadAllText(options.inFile);
                var processed = Preprocessor.Preprocess(source, scriptEngine);

                var destination = System.IO.File.Open(options.outFile, System.IO.FileMode.Create);
                var writer = new System.IO.StreamWriter(destination);

                if (options.mode == "pre")
                    writer.Write(processed);
                else if (options.mode == "extract-prose")
                    foreach (var item in new ExtractProse(Parser.Parse(processed)))
                        writer.WriteLine(item);
                
                writer.Flush();
                destination.Flush();
                destination.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured.");
                Console.WriteLine(e.Message);
            }
        }

    }
}
