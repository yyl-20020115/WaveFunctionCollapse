// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System;

abstract class Model(int width, int height, int N, bool periodic, Model.Heuristic heuristic)
{
    protected bool[][] wave;

    protected int[][][] propagator;
    int[][][] compatible;
    protected int[] observed;

    (int, int)[] stack;
    int stacksize;
    int observedSoFar;

    protected int MX = width;
    protected int MY = height;
    protected int T;
    protected int N = N;
    protected bool periodic = periodic;
    protected bool ground;

    protected double[] weights;
    double[] weightLogWeights;
    double[] distribution;

    protected int[] sumsOfOnes;
    double sumOfWeights;
    double sumOfWeightLogWeights;
    double startingEntropy;
    protected double[] sumsOfWeights;
    protected double[] sumsOfWeightLogWeights;
    protected double[] entropies;

    public enum Heuristic : int
    { 
        Entropy, 
        MRV, 
        Scanline 
    };

    private readonly Heuristic heuristic = heuristic;

    void Init()
    {
        this.wave = new bool[this.MX * this.MY][];
        this.compatible = new int[this.wave.Length][][];
        for (int i = 0; i < this.wave.Length; i++)
        {
            this.wave[i] = new bool[this.T];
            this.compatible[i] = new int[this.T][];
            for (int t = 0; t < this.T; t++)
                this.compatible[i][t] = new int[4];
        }
        this.distribution = new double[this.T];
        this.observed = new int[this.MX * this.MY];

        this.weightLogWeights = new double[this.T];
        this.sumOfWeights = 0;
        this.sumOfWeightLogWeights = 0;

        for (int t = 0; t < T; t++)
        {
            this.weightLogWeights[t] 
                = this.weights[t] * Math.Log(this.weights[t]);
            this.sumOfWeights 
                += this.weights[t];
            this.sumOfWeightLogWeights 
                += this.weightLogWeights[t];
        }

        this.startingEntropy 
            = Math.Log(this.sumOfWeights) 
            - this.sumOfWeightLogWeights 
            / sumOfWeights;

        this.sumsOfOnes = new int[this.MX * this.MY];
        this.sumsOfWeights = new double[this.MX * this.MY];
        this.sumsOfWeightLogWeights = new double[this.MX * this.MY];
        this.entropies = new double[this.MX * this.MY];

        this.stack = new (int, int)[this.wave.Length * this.T];
        this.stacksize = 0;
    }

    public bool Run(int seed, int limit)
    {
        if (this.wave == null)
            this.Init();

        this.Clear();
        var random = new Random(seed);

        for (int l = 0; l < limit || limit < 0; l++)
        {
            int node = this.NextUnobservedNode(random);
            if (node >= 0)
            {
                this.Observe(node, random);
                var success = this.Propagate();
                if (!success) return false;
            }
            else
            {
                for (int i = 0; i < this.wave.Length; i++)
                {
                    for (int t = 0; t < T; t++)
                    {
                        if (this.wave[i][t])
                        {
                            this.observed[i] = t;
                            break;
                        }
                    }
                }
                return true;
            }
        }

        return true;
    }

    int NextUnobservedNode(Random random)
    {
        if (this.heuristic == Heuristic.Scanline)
        {
            for (int i = this.observedSoFar; i < this.wave.Length; i++)
            {
                if (!this.periodic && (i % this.MX + this.N > this.MX || i / this.MX + this.N > this.MY))
                    continue;
                if (this.sumsOfOnes[i] > 1)
                {
                    this.observedSoFar = i + 1;
                    return i;
                }
            }
            return -1;
        }

        var min = 1E+4;
        var argmin = -1;
        for (int i = 0; i < this.wave.Length; i++)
        {
            if (!periodic && (i % this.MX + this.N > this.MX || i / this.MX + this.N > this.MY)) continue;
            var remainingValues = this.sumsOfOnes[i];
            var entropy 
                = heuristic == Heuristic.Entropy
                ? this.entropies[i]
                : remainingValues;
            if (remainingValues > 1 && entropy <= min)
            {
                var noise = 1E-6 * random.NextDouble();
                if (entropy + noise < min)
                {
                    min = entropy + noise;
                    argmin = i;
                }
            }
        }
        return argmin;
    }

    void Observe(int node, Random random)
    {
        var w = this.wave[node];
        for (int t = 0; t < T; t++)
            this.distribution[t] = w[t] ? this.weights[t] : 0.0;
        var r = distribution.Random(random.NextDouble());
        for (int t = 0; t < T; t++)
            if (w[t] != (t == r)) 
                this.Ban(node, t);
    }

    bool Propagate()
    {
        while (this.stacksize > 0)
        {
            (int i1, int t1) = this.stack[this.stacksize - 1];
            this.stacksize--;

            var x1 = i1 % this.MX;
            var y1 = i1 / this.MX;

            for (var d = 0; d < 4; d++)
            {
                var x2 = x1 + dx[d];
                var y2 = y1 + dy[d];
                if (!this.periodic 
                    && (x2 < 0 || y2 < 0 || x2 + N > this.MX || y2 + this.N > this.MY))
                    continue;

                if (x2 < 0) 
                    x2 += this.MX;
                else if (x2 >= this.MX) 
                    x2 -= this.MX;
                if (y2 < 0)
                    y2 += this.MY;
                else if (y2 >= this.MY) 
                    y2 -= this.MY;

                var i2 = x2 + y2 * this.MX;
                var p = this.propagator[d][t1];
                var compat = this.compatible[i2];

                for (var l = 0; l < p.Length; l++)
                {
                    var t2 = p[l];
                    var comp = compat[t2];

                    comp[d]--;
                    if (comp[d] == 0)
                        this.Ban(i2, t2);
                }
            }
        }

        return this.sumsOfOnes[0] > 0;
    }

    void Ban(int i, int t)
    {
        this.wave[i][t] = false;
        var comp = this.compatible[i][t];
        for (int d = 0; d < 4; d++)
            comp[d] = 0;
        this.stack[this.stacksize] = (i, t);
        this.stacksize++;

        this.sumsOfOnes[i] -= 1;
        this.sumsOfWeights[i] -= this.weights[t];
        this.sumsOfWeightLogWeights[i] -= this.weightLogWeights[t];

        var sum = this.sumsOfWeights[i];
        entropies[i] = Math.Log(sum) - this.sumsOfWeightLogWeights[i] / sum;
    }

    void Clear()
    {
        for (int i = 0; i < this.wave.Length; i++)
        {
            for (int t = 0; t < T; t++)
            {
                this.wave[i][t] = true;
                for (int d = 0; d < 4; d++)
                    this.compatible[i][t][d]
                        = this.propagator[opposite[d]][t].Length;
            }

            this.sumsOfOnes[i] = this.weights.Length;
            this.sumsOfWeights[i] = this.sumOfWeights;
            this.sumsOfWeightLogWeights[i] = this.sumOfWeightLogWeights;
            this.entropies[i] = this.startingEntropy;
            this.observed[i] = -1;
        }
        this.observedSoFar = 0;

        if (ground)
        {
            for (int x = 0; x < this.MX; x++)
            {
                for (int t = 0; t < T - 1; t++)
                    this.Ban(x + (this.MY - 1) * this.MX, t);
                for (int y = 0; y < this.MY - 1; y++)
                    this.Ban(x + y * this.MX, this.T - 1);
            }
            this.Propagate();
        }
    }

    public abstract void Save(string filename);

    protected static int[] dx = [-1, 0, 1, 0];
    protected static int[] dy = [0, 1, 0, -1];
    private static readonly int[] opposite = [2, 3, 0, 1];
}
