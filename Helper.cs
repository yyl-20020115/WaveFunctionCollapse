// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System.Linq;
using System.Xml.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

static class Helper
{
    public static int Random(this double[] weights, double r)
    {
        double sum = 0;
        for (int i = 0; i < weights.Length; i++) 
            sum += weights[i];

        var threshold = r * sum;
        double partialSum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            partialSum += weights[i];
            if (partialSum >= threshold)
                return i;
        }
        return 0;
    }

    public static long ToPower(this int a, int n)
    {
        long product = 1;
        for (int i = 0; i < n; i++)
            product *= a;
        return product;
    }

    public static T Get<T>(this XElement element, string attribute, T defaultT = default)
    {
        var a = element.Attribute(attribute);
        return a == null ? defaultT : (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(a.Value);
    }

    public static IEnumerable<XElement> Elements(this XElement element, params string[] names) 
        => element.Elements().Where(e => names.Any(n => n == e.Name));
}

static class BitmapHelper
{
    public static (int[], int, int) LoadBitmap(string filename)
    {
        using var image = Image.Load<Bgra32>(filename);
        var width = image.Width;
        var height = image.Height;
        var result = new int[width * height];
        image.CopyPixelDataTo(MemoryMarshal.Cast<int, Bgra32>(result));
        return (result, width, height);
    }

    unsafe public static void SaveBitmap(int[] data, int width, int height, string filename)
    {
        using var image = Image.LoadPixelData<Rgba32>(
            MemoryMarshal.Cast<int, Rgba32>(data),
            width, 
            height);
        image.SaveAsPng(filename);
    }
}
