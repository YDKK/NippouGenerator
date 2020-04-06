using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NippouGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = args.FirstOrDefault();
            if (!File.Exists(path))
            {
                Console.WriteLine("NippouGenerator v0.3");
                Console.WriteLine("usage: NippouGenerator.exe <path>");
                return;
            }

            var input = File.ReadAllLines(path);
            var root = new Content { Level = Level.root };
            Stack<Content> lastContent = new Stack<Content>();

            foreach (var line in input)
            {
                var level = LevelDefinition.First(x => line.StartsWith(x.Value.from));

                var content = new Content
                {
                    Level = level.Key,
                    Text = line.Substring(level.Value.from.Length)
                };

                while (lastContent.TryPeek(out var lc) && lc.Level >= level.Key)
                {
                    lastContent.Pop();
                }
                lastContent.TryPeek(out var last);

                switch (level.Key)
                {
                    case Level.見出し:
                        root.Children.Add(content);
                        lastContent.Push(content);
                        continue;
                    case Level.項目:
                    case Level.列挙1:
                        last.Children.Add(content);
                        lastContent.Push(content);
                        continue;
                    case Level.列挙2:
                        if (last.Level == Level.列挙2)
                        {
                            lastContent.Pop();
                        }
                        lastContent.Peek().Children.Add(content);
                        lastContent.Push(content);
                        continue;
                    case Level.文:
                        last.Children.Add(content);
                        if (string.IsNullOrEmpty(line) && last.Level == Level.列挙2)
                        {
                            lastContent.Pop();
                        }
                        continue;
                }
            }

            var result = GenerateOutput(root, 0, 0).Substring(2);

            Console.WriteLine(result);
            Console.WriteLine();
            Console.WriteLine("以上");
        }

        public const string Crlf = "\r\n";

        public enum Level
        {
            root = -1,
            見出し,
            項目,
            列挙1,
            列挙2,
            文
        }

        public static readonly Dictionary<Level, (string from, string to, string indent)> LevelDefinition = new Dictionary<Level, (string from, string to, string indent)>
        {
            { Level.見出し, ("# ",  "■１．", "　　　") },
            { Level.項目,   ("## ", "（１）", "　　　") },
            { Level.列挙1,  ("1. ", "(a) ", "　　") },
            { Level.列挙2,  ("- ",  "- ", "　") },
            { Level.文,     ("", "", "") },
        };

        public static readonly Dictionary<char, string> IndexNumDefinition = new Dictionary<char, string>
        {
            {'１', "―１２３４５６７８９" },
            {'a', "-abcdefghijklmnopqrstuvwxyz" },
        };


        public class Content
        {
            public Level Level { get; set; }
            public string Text { get; set; }
            public List<Content> Children { get; set; } = new List<Content>();
        }

        public static string GenerateOutput(Content content, int currentIndent, int indexNum)
        {
            var result = "";
            if (content.Level == Level.root)
            {
                currentIndent = -1;
                goto next;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var sjisEnc = Encoding.GetEncoding("Shift_JIS");

            var levelText = LevelDefinition[content.Level].to;
            foreach (var num in IndexNumDefinition)
            {
                levelText = levelText.Replace(num.Key, num.Value[indexNum]);
            }

            if (content.Level == Level.項目)
            {
                currentIndent--;
            }

            var indent = "　　　　　　　　　　".Substring(0, currentIndent);
            var indent2 = indent + LevelDefinition[content.Level].indent;
            result += indent;


            result += levelText;
            result += content.Text;
            while (sjisEnc.GetByteCount(result.Split(Crlf).Last()) > 80)
            {
                var str = "";
                var res = result.Split(Crlf).Last();

                while (sjisEnc.GetByteCount(str) <= 78 && res.Any())
                {
                    str += res.First();
                    res = res.Substring(1);
                }

                if (res.Any())
                {
                    str += Crlf + indent2;
                    result = result.Substring(0, result.Contains(Crlf) ? result.LastIndexOf(Crlf) + 2 : 0) + str + res;
                }
            }

        next:
            indexNum = 0;
            foreach (var child in content.Children)
            {
                if (child.Level != Level.文)
                {
                    indexNum++;
                }
                result += Crlf;
                result += GenerateOutput(child, currentIndent + 1, indexNum);
            }

            return result;
        }
    }
}
