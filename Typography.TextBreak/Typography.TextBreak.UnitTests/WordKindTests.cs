﻿using System.Collections.Generic;
using NUnit.Framework;
using Typography.TextBreak;
using static Typography.TextBreak.WordKind;

public class WordKindTests
{
    public void WordKindTest(string input, (string section, WordKind wordKind)[] output)
    {
        var customBreaker = new CustomBreaker();
        var outputList = new List<BreakAtInfo> { new BreakAtInfo(0, Unknown) };
        customBreaker.SetNewBreakHandler(vis => outputList.Add(new BreakAtInfo(vis.LatestBreakAt, vis.LatestWordKind)));

        customBreaker.BreakWords(input);

        for (int i = 0; i < outputList.Count - 1; i++)
        {
            Assert.AreEqual
            (
                output[i].section,
                input.Substring(outputList[i].breakAt,
                                outputList[i + 1].breakAt - outputList[i].breakAt)
            );

            Typography.TextBreak.WordKind w0 = output[i].wordKind;
            Typography.TextBreak.WordKind w1 = outputList[i + 1].wordKind;
            if (w0 != w1)
            {

            }

            Assert.AreEqual(output[i].wordKind, outputList[i + 1].wordKind);
        }
    }

    [Test]
    public void WordKind()
    {
        foreach (var testCase in new[] {

           
          ("Hi!", new [] { ("Hi", Text), ("!", Punc) }),

          ("We are #1", new[] { ("We", Text), (" ", Whitespace),
              ("are", Text), (" ", Whitespace), ("#", Punc),
              ("1", Number) }),

          ("1337 5P34K", new[] { ("1337", Number), (" ", Whitespace),
              ("5", Number), ("P34K", Text) }),


          ("In\u000Bbetween\u000Care\u0020spaces", new[] {
              ("In", Text), ("\u000B", OtherWhitespace), ("between", Text),
              ("\u000C", OtherWhitespace), ("are", Text), ("\u0020", Whitespace),
              ("spaces", Text) }),

          ("!@#$%^&*()", new[] { ("!", Punc), ("@", Punc), ("#", Punc),
              ("$", Punc), ("%", Punc), ("^", Punc), ("&", Punc), ("*", Punc),
              ("(", Punc), (")", Punc) }),

          ("1st line\r2nd line\n3rd line\r\n4th line\u00855th line",
             new[] { ("1", Number), ("st", Text), (" ", Whitespace), ("line", Text),
                 ("\r", NewLine), ("2", Number), ("nd", Text), (" ", Whitespace),
                 ("line", Text), ("\n", NewLine),
                 ("3", Number), ("rd", Text), (" ", Whitespace), ("line", Text),
                 ("\r\n", NewLine), ("4", Number), ("th", Text), (" ", Whitespace),
                 ("line", Text), ("\u0085", NewLine),
                 ("5", Number), ("th", Text), (" ", Whitespace), ("line", Text) })
        })
        {
            WordKindTest(testCase.Item1, testCase.Item2);
        }
    }
}