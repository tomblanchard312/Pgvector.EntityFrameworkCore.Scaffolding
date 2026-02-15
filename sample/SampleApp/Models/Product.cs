using System;
using System.Collections.Generic;
using Pgvector;

namespace SampleApp.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Category { get; set; }

    public decimal Price { get; set; }

    public Vector? Embedding { get; set; }
}
