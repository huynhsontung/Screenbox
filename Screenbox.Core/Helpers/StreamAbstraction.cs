using System.IO;

namespace Screenbox.Core.Helpers;
internal class StreamAbstraction : TagLib.File.IFileAbstraction
{
    public StreamAbstraction(string name, Stream readStream)
    {
        Name = name;
        ReadStream = readStream;
        WriteStream = Stream.Null;
    }

    public StreamAbstraction(string name, Stream readStream, Stream writeStream)
    {
        Name = name;
        ReadStream = readStream;
        WriteStream = writeStream;
    }

    public void CloseStream(Stream stream)
    {
        stream.Close();
    }

    public string Name { get; }
    public Stream ReadStream { get; }
    public Stream WriteStream { get; }
}
