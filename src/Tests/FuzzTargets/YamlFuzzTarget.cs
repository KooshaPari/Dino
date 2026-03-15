#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using SharpFuzz;
using Xunit;

namespace DINOForge.Tests.FuzzTargets
{
    public static class YamlFuzzTarget
    {
        internal static void FuzzAction(ReadOnlySpan<byte> data)
        {
            try
            {
                string yaml = System.Text.Encoding.UTF8.GetString(data);
                YamlDotNet.Serialization.IDeserializer deserializer =
                    new YamlDotNet.Serialization.DeserializerBuilder().Build();
                deserializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(yaml);
            }
            catch (YamlDotNet.Core.YamlException) { }
            catch (Exception e) when (e is not OutOfMemoryException && e is not StackOverflowException) { }
        }

        internal static void RunLibFuzzer() => Fuzzer.LibFuzzer.Run(FuzzAction);

        internal static void RunAfl()
        {
            Fuzzer.OutOfProcess.Run(stream =>
            {
                using MemoryStream ms = new MemoryStream();
                stream.CopyTo(ms);
                FuzzAction(ms.ToArray());
            });
        }

        [Theory]
        [Trait("Category", "Fuzz")]
        [InlineData("id: test
name: Test Pack
version: 0.1.0")]
        [InlineData("")]
        [InlineData("not yaml at all: [[[")]
        public static void Smoke_DoesNotThrow(string yamlInput)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(yamlInput);
            FuzzAction(bytes);
        }
    }
}
