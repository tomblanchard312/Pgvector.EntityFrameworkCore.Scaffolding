using System;
using System.Collections.Generic;
using Pgvector;

namespace SampleApp.Models;

public partial class Document
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    public Vector? Embedding { get; set; }
}
