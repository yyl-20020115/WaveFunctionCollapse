// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System;
using System.Xml.Linq;
using System.Diagnostics;

static class Program
{
    static void Main()
    {
        const string fn = "output";
        var watch = Stopwatch.StartNew();
        if(System.IO.Directory.Exists(fn))
            System.IO.Directory.Delete(fn,true);
        var folder = System.IO.Directory.CreateDirectory(fn);
        //foreach (var file in folder.GetFiles()) 
        //    file.Delete();

        Random random = new();
        var xdoc = XDocument.Load("samples.xml");

        foreach (var xelem in xdoc.Root.Elements("overlapping", "simpletiled"))
        {
            Model model;
            var name = xelem.Get<string>("name");
            Console.WriteLine($"< {name}");

            var isOverlapping = xelem.Name == "overlapping";
            var size = xelem.Get("size", isOverlapping ? 48 : 24);
            var width = xelem.Get("width", size);
            var height = xelem.Get("height", size);
            var periodic = xelem.Get("periodic", false);
            var heuristicString = xelem.Get<string>("heuristic");
            var heuristic = heuristicString == "Scanline" ? Model.Heuristic.Scanline : (heuristicString == "MRV" ? Model.Heuristic.MRV : Model.Heuristic.Entropy);

            if (isOverlapping)
            {
                var N = xelem.Get("N", 3);
                var periodicInput = xelem.Get("periodicInput", true);
                var symmetry = xelem.Get("symmetry", 8);
                var ground = xelem.Get("ground", false);

                model = new OverlappingModel(name, N, width, height, periodicInput, periodic, symmetry, ground, heuristic);
            }
            else
            {
                var subset = xelem.Get<string>("subset");
                var blackBackground = xelem.Get("blackBackground", false);

                model = new SimpleTiledModel(name, subset, width, height, periodic, blackBackground, heuristic);
            }

            for (int i = 0; i < xelem.Get("screenshots", 2); i++)
            {
                for (int k = 0; k < 10; k++)
                {
                    Console.Write("> ");
                    var seed = random.Next();
                    var success = model.Run(seed, xelem.Get("limit", -1));
                    if (success)
                    {
                        Console.WriteLine("DONE");
                        model.Save($"output/{name} {seed}.png");
                        if (model is SimpleTiledModel stmodel && xelem.Get("textOutput", false))
                            System.IO.File.WriteAllText($"output/{name} {seed}.txt", stmodel.TextOutput());
                        break;
                    }
                    else Console.WriteLine("CONTRADICTION");
                }
            }
        }

        Console.WriteLine($"time = {watch.ElapsedMilliseconds}");
    }
}
