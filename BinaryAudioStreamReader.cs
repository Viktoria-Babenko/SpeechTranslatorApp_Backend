using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;

public class BinaryAudioStreamReader : PullAudioInputStreamCallback
{
	private readonly Stream _stream;
	public BinaryAudioStreamReader(Stream stream)
	{
		_stream = stream;
	}

	public override int Read(byte[] dataBuffer, uint size)
	{
		return _stream.Read(dataBuffer, 0, (int)size);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_stream.Dispose();
		}
		base.Dispose(disposing);
	}
}

