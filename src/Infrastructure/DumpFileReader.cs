using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace EliteBuckyball.Infrastructure
{
    public class DumpFileReader<T> : IEnumerable<T> 
    {

        private List<T> list = new List<T>();

        public DumpFileReader(string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open);
            using var zipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            {
                list = JsonSerializer.DeserializeAsync<List<T>>(zipStream).Result;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

    }
}
