// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WaveFunctionCollapse;

public static class BitmapHelper
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
