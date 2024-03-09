using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlacHiderLib
{
    public static class BitStreamEnumerableExtensions
    {
        public static IEnumerable<bool> ToBitEnumerable(this IEnumerable<byte> source) => source.SelectMany(@byte => Enumerable.Range(0, 8)
            .Reverse()
            .Select(pow => (int)Math.Floor(Math.Pow(2, pow)))
            .Select(mask => (@byte & mask) == 0)
            );

        

        
        public static IEnumerable<byte> TobyteEnumerable(this IEnumerable<bool> source) => source
            .Chunk(8)
            .Select(bits => bits.Aggregate(0, (prev, curbit) => prev * 2 + (curbit ? 1 : 0)
            , (fin) => (byte)fin
            ));


        public static IEnumerable<bool> LSBBitEnumerable(this IEnumerable<bool> source) => source
            .Chunk(16)
            .Select(chunk => chunk.Skip(7).First());
        public static IEnumerable<bool> LSBBitEnumerableWithSetLSB(this IEnumerable<bool> source, IEnumerable<bool> new_lsb) => source
            .Chunk(16)
            .Zip(new_lsb, (chunk, new_lsb_element) => chunk.Take(7).Append(new_lsb_element).Concat(chunk.Skip(8)))
            .SelectMany(b => b);
    }
}
