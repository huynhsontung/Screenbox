using System;

namespace Screenbox.Core.Models;

public interface IMediaProperties
{
    string Title { get; set; }
    uint Year { get; set; }
    TimeSpan Duration { get; set; }
}
