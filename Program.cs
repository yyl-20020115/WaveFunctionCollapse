// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO;

static class Program
{
    static void Main()
    {
        const string fn = "output";
        var watch = Stopwatch.StartNew();
        if(Directory.Exists(fn))
            Directory.Delete(fn,true);
        Directory.CreateDirectory(fn);
        
        var random = new Random();
        var document = XDocument.Load("samples.xml");

        foreach (var element in document.Root.Elements("overlapping", "simpletiled"))
        {
            Model model;
            var name = element.Get("name", string.Empty);
            Console.WriteLine($"< {name}");

            var isOverlapping = element.Name == "overlapping";
            var size = element.Get("size", isOverlapping ? 48 : 24);
            var width = element.Get("width", size);
            var height = element.Get("height", size);
            var periodic = element.Get("periodic", false);
            var heuristicString = element.Get("heuristic",string.Empty);
            var heuristic = heuristicString == "Scanline"
                ? Model.Heuristic.Scanline 
                : (heuristicString == "MRV" 
                ? Model.Heuristic.MRV 
                : Model.Heuristic.Entropy)
                ;

            if (isOverlapping)
            {
                var N = element.Get("N", 3);
                var periodicInput = element.Get("periodicInput", true);
                var symmetry = element.Get("symmetry", 8);
                var ground = element.Get("ground", false);

                model = new OverlappingModel(name, N, width, height, periodicInput, periodic, symmetry, ground, heuristic);
            }
            else
            {
                var subset = element.Get("subset", string.Empty);
                var blackBackground = element.Get("blackBackground", false);

                model = new SimpleTiledModel(name, subset, width, height, periodic, blackBackground, heuristic);
            }

            for (int i = 0; i < element.Get("screenshots", 2); i++)
            {
                for (int k = 0; k < 10; k++)
                {
                    Console.Write("> ");
                    var seed = random.Next();
                    var success = model.Run(seed, element.Get("limit", -1));
                    if (success)
                    {
                        Console.WriteLine("DONE");
                        model.Save($"output/{name}-{seed}.png");
                        if (model is SimpleTiledModel stmodel && element.Get("textOutput", false))
                            File.WriteAllText($"output/{name}-{seed}.txt", stmodel.TextOutput());
                        break;
                    }
                    else
                    {
                        Console.WriteLine("CONTRADICTION");
                    }
                }
            }
        }

        Console.WriteLine($"time = {watch.ElapsedMilliseconds}");
    }
}
