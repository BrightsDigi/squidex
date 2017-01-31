﻿// ==========================================================================
//  Program.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var languageCodesFile = new FileInfo("../../src/Squidex.Infrastructure/language-codes.csv");
            var languageFile = Path.Combine(languageCodesFile.DirectoryName, "Languages.cs");

            var resourceStream = new FileStream(languageCodesFile.FullName, FileMode.Open);

            var writer = new StringWriter();
            writer.WriteLine("// ==========================================================================");
            writer.WriteLine("//  Langauges.cs");
            writer.WriteLine("//  Squidex Headless CMS");
            writer.WriteLine("// ==========================================================================");
            writer.WriteLine("//  Copyright (c) Squidex Group");
            writer.WriteLine("//  All rights reserved.");
            writer.WriteLine("// ==========================================================================");
            writer.WriteLine();
            writer.WriteLine("namespace Squidex.Infrastructure");
            writer.WriteLine("{");
            writer.WriteLine("    partial class Language");
            writer.WriteLine("    {");

            var uniqueCodes = new HashSet<string>(new [] { "iv" });
            
            using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
            {
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    var languageLine = reader.ReadLine();
                    var languageIso2Code = languageLine.Substring(1, 2);
                    var languageEnglishName = languageLine.Substring(6, languageLine.Length - 7);

                    if (!uniqueCodes.Add(languageIso2Code))
                    {
                        Console.WriteLine("Languages contains duplicate {0}", languageIso2Code);
                    } 

                    writer.WriteLine("        public static Language {0} = AddLanguage(\"{1}\", \"{2}\");", languageIso2Code.ToUpperInvariant(), languageIso2Code, languageEnglishName);
                }
            }

            writer.WriteLine("    }");
            writer.WriteLine("}");

            File.WriteAllText(languageFile, writer.ToString());
        }
    }
}
