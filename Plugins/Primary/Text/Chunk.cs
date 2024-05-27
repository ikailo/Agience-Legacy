using Agience.SDK;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace Agience.Plugins.Primary.Text
{
    internal class Chunk
    {
        private const int DEFAULT_SIZE = 4000;

        //public override string[] InputKeys => ["text", "size:int"];
        //public override string[] OutputKeys => ["chunks:string[]"];

        [KernelFunction, Description("Split text into chunks.")]
        public Task<Data?> Process(Runner runner, Data? input = null)
        {
            string? text = string.Empty;
            int size = DEFAULT_SIZE;

            if (input?["text"] != null)
            {
                text = input?["text"];
                size = int.TryParse(input?["size"], out size) ? size : DEFAULT_SIZE;
            }
            else
            {
                text = input;
            }

            return Task.FromResult<Data?>(new()
            {
                { "chunks", JsonSerializer.Serialize(SplitText(text ?? string.Empty, size)) }
            });
        }

        public static string[] SplitText(string text, int maxLength = DEFAULT_SIZE)
        {
            List<string> result = new List<string>();
            int start = 0;
            while (start < text.Length)
            {
                int length = Math.Min(maxLength, text.Length - start);
                string substr = text.Substring(start, length);

                // if the substring ends in the middle of a sentence, adjust the length accordingly
                if (substr.LastIndexOfAny(new char[] { '.', '!', '?' }) != substr.Length - 1)
                {
                    int lastPeriod = substr.LastIndexOf('.');
                    int lastExclamation = substr.LastIndexOf('!');
                    int lastQuestion = substr.LastIndexOf('?');
                    int lastEnd = Math.Max(lastPeriod, Math.Max(lastExclamation, lastQuestion));
                    if (lastEnd != -1)
                    {
                        length = lastEnd + 1;
                        substr = text.Substring(start, length);
                    }
                }

                result.Add(substr);
                start += length;
            }
            return result.ToArray();
        }
    }
}
