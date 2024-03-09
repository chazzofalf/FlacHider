using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlacHiderLib
{
    internal class EnumerableStream : Stream
    {
        private IEnumerable<byte> Source { get; }
        private IEnumerator<byte> SourceEnumerator { get; }
        public EnumerableStream(IEnumerable<byte> Source)
        {
            this.Source = Source;
            this.SourceEnumerator = Source.GetEnumerator();
        }
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => throw new IOException("Cannot get length!");

        public override long Position { get => throw new IOException("Cannot get position!"); set => throw new IOException("Cannot set position!"); }

        public override void Flush()
        {
            throw new IOException("Cannot write!"); ;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {                        
            for (int i = offset; i < count+offset; i++)
            {
                if (SourceEnumerator.MoveNext())
                {
                    buffer[i] = SourceEnumerator.Current;
                }
                else
                {
                    return i - offset;
                }
            }
            return count;
            
        }
        private static IEnumerable<long> Range(long start,long count)
        {
            long i = start;
            while (i < i+count)
            {
                yield return i;
                i++;
            }
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin.HasFlag(SeekOrigin.Begin))
            {
                SourceEnumerator.Reset();
                
            }
            if (!origin.HasFlag(SeekOrigin.End))
            {
                Range(0L, offset)
                    .ForEachOverEnumerable(i =>
                    {
                        if (!SourceEnumerator.MoveNext())
                        {
                            throw new IOException("Seek out of range!");
                        }
                    });
                return 0L;
            }
            else
            {
                throw new IOException("Backward reading not supported!");
            }
                    
        }

        public override void SetLength(long value)
        {
            throw new IOException("Cannot write!"); ;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new IOException("Cannot write!"); ;
        }
    }
    internal class StreamEnumerable : IEnumerable<byte>
    {
        private Stream Stream { get; }
        public StreamEnumerable(Stream stream)
        {
            this.Stream = stream;
        }
        public IEnumerator<byte> GetEnumerator()
        {
            return new StreamEnumerator(Stream);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    internal static class StreamEnumerableConstants
    {
        public const int MAX_BUFFER_SIZE = 0x100000;
        public const int SMALL_BUFFER_SIZE = MAX_BUFFER_SIZE / 0x100;
    }
    internal class StreamEnumerator : IEnumerator<byte>
    {
        
        private byte[] single = new byte[1];
        
        private Stream Stream { get; }
        private MemoryStream Buffer { get; } = new MemoryStream();
        public byte Current { get; private set; }

        object IEnumerator.Current => Current;

        public StreamEnumerator(Stream stream)
        {
            this.Stream = stream;
        }
        private void ClearBuffer()
        {
            Buffer.Position = 0;
            Buffer.SetLength(0);
        }

        private void FillBuffer()
        {
            ClearBuffer();
            var buffer = new byte[StreamEnumerableConstants.SMALL_BUFFER_SIZE];
            var len = Stream.Read(buffer);
            while (len > 0)
            {
                Buffer.Write(buffer, 0, len);
                if (Buffer.Length >= StreamEnumerableConstants.MAX_BUFFER_SIZE)
                {
                    Buffer.Position = 0;
                    break;
                }
                len = Stream.Read(buffer);
            }
        }
        private bool TryReadOne()
        {
            return Buffer.Read(single) > 0 || ((Func<bool>)(() =>
                {
                FillBuffer();
                return Buffer.Read(single) > 0;
            }))();            
        }
        public bool MoveNext()
        {
            if (TryReadOne())
            {
                Current = single[0];
                return true;
            }
            return false;

        }

        public void Reset()
        {
            Stream.Seek(0, SeekOrigin.Begin);
            ClearBuffer();

        }

        public void Dispose()
        {
            
        }
    }
    public static class ByteEnumerableStreamExtensions
    {
        public static void ForEachOverEnumerable<T>(this IEnumerable<T> @enum,Action<T> forEachAction,Action? finisher=null)
        {
            @enum.Aggregate((object?)null, (_, @element) =>
            {
                forEachAction(element);
                return null;
            },(_) =>
            {
                if (finisher != null)
                {
                    finisher();
                }
                return _;
            });
        }

        public static void WriteToStream(this IEnumerable<byte> me,Stream stream)
        {
            var bbuf = new MemoryStream();
            var buf = new byte[StreamEnumerableConstants.SMALL_BUFFER_SIZE];
            var single = new byte[1];            
            me.ForEachOverEnumerable(b =>
            {                
                single[0] = b;
                bbuf.Write(single);
                if (bbuf.Length >= StreamEnumerableConstants.MAX_BUFFER_SIZE)
                {
                    bbuf.Position = 0;
                    var len = bbuf.Read(buf);
                    while (len > 0)
                    {
                        stream.Write(buf,0,len);
                        len = bbuf.Read(buf);
                    }
                    bbuf.Position = 0;
                    bbuf.SetLength(0);
                }
            },() =>
            {
                if (bbuf.Length > 0)
                {
                    bbuf.Position = 0;
                    var len = bbuf.Read(buf);
                    while (len > 0)
                    {
                        stream.Write(buf, 0, len);
                        len = bbuf.Read(buf);
                    }
                    bbuf.Position = 0;
                    bbuf.SetLength(0);
                }
            });
        }
        public static IEnumerable<byte> WrapStreamInEnumerable(this Stream stream)
        {
            return new StreamEnumerable(stream);
        }
    }
}
