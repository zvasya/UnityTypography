using System.Collections.Generic;
using Typography.TextBreak;
using NUnit.Framework;
using UnityEngine.TestTools;


public class TestOptions
{
    public bool BreakNumberAfterText { get; set; }
    public SurrogatePairBreakingOption SurrogatePairBreakingOption { get; set; } = SurrogatePairBreakingOption.OnlySurrogatePair;
}

public class BasicTests
{


    public void BasicTest(string input, string[] output, TestOptions options = null)
    {
        if (options == null)
        {
            options = new TestOptions();
        }

        var outputList = new List<int> { 0 };
        var customBreaker = new CustomBreaker();
        customBreaker.SetNewBreakHandler(vis => outputList.Add(vis.LatestBreakAt));
        //options
        customBreaker.BreakNumberAfterText = options.BreakNumberAfterText;
        customBreaker.EngBreakingEngine.SurrogatePairBreakingOption = options.SurrogatePairBreakingOption;

        //
        customBreaker.BreakWords(input);
        //customBreaker.CopyBreakResults(outputList);
        for (int i = 0; i < outputList.Count - 1; i++)
        {
            Assert.AreEqual
            (
                output[i],
                input.Substring(outputList[i], outputList[i + 1] - outputList[i])
            );
        }
    }


    [Test]
    [TestCase("Hi!", 0, new[] { "Hi", "!" })]
    [TestCase("We are #1", 0, new[] { "We", " ", "are", " ", "#", "1" })]
    [TestCase("1337 5P34K", 0, new[] { "1337", " ", "5", "P34K" })]
    [TestCase("ša bčš čš", 0, new[] { "ša", " ", "bčš", " ", "čš" })]
    [TestCase("ščěěščž čšřžščřž čšřžščř", 0, new[] { "ščěěščž", " ", "čšřžščřž", " ", "čšřžščř" })]
    [TestCase("!@#$%^&*()", 0, new[] { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")" })]
    [TestCase("1st line\r2nd line\n3rd line\r\n4th line\u00855th line", 0,
        new[] { "1", "st", " ", "line", "\r", "2", "nd", " ", "line", "\n",
                "3", "rd", " ", "line", "\r\n", "4", "th", " ", "line", "\u0085",
                "5", "th", " ", "line" })]
    [TestCase("6+23-456*78/9", 0, new[] { "6", "+", "23", "-456", "*", "78", "/", "9" })]
    [TestCase("<>_____DisplayClass", 0, new[] { "<", ">", "_", "_", "_", "_", "_", "DisplayClass" })]
    [TestCase("In\u000Bbetween\u000Care\u0020spaces", 0,
        new[] { "In", "\u000B", "between", "\u000C", "are", "\u0020", "spaces" })]
    public void Basic(string input, int _, string[] output) => BasicTest(input, output);

    [Test]
    [TestCase("\0\x1\x2\x3\x4\x5\x6\x7\x8\x9", 0,
        new[] { "\0", "\x1", "\x2", "\x3", "\x4", "\x5", "\x6", "\x7", "\x8", "\x9" })]
    public void Control(string input, int _, string[] output) => BasicTest(input, output);

    [Test]
    [TestCase("\u0100", 0, new[] { "\u0100" })]
    [TestCase("\u3DB4", 0, new[] { "\u3DB4" })]
    [TestCase("\uFFFF", 0, new[] { "\uFFFF" })]
    [TestCase("\r\n‸", 0, new[] { "\r\n", "‸" })]
    [TestCase("\r\n‸\r\n", 0, new[] { "\r\n", "‸", "\r\n" })]
    [TestCase("\r\n‸12a\r\n", 0, new[] { "\r\n", "‸", "12", "a", "\r\n" })]
    public void OutOfRange(string input, int _, string[] output) => BasicTest(input, output);

    [Test]
    [TestCase("\t\t", 0, new[] { "\t\t" })]
    [TestCase("\t\t\t\t  \t\t\t\t", 0, new[] { "\t\t\t\t", "  ", "\t\t\t\t" })]
    [TestCase("a\t\tb", 0, new[] { "a", "\t\t", "b" })]
    [TestCase("a\t \tb", 0, new[] { "a", "\t", " ", "\t", "b" })]
    [TestCase("\t\ta\t\tb", 0, new[] { "\t\t", "a", "\t\t", "b" })]
    public void WhitespacesAndTabs(string input, int _, string[] output) => BasicTest(input, output);


    [Test]
    [TestCase("😀", 0, new[] { "😀" })]
    [TestCase("😂", 0, new[] { "😂" })]
    [TestCase("😂😂", 0, new[] { "😂", "😂" })]
    [TestCase("😂A😂", 0, new[] { "😂", "A", "😂" })]
    [TestCase("😂A123😂", 0, new[] { "😂", "A123", "😂" })]
    public void Surrogates(string input, int _, string[] output) => BasicTest(input, output);

    [Test]
    [TestCase("👩🏾‍👨🏾‍👧🏾‍👶🏾", 0, new[] { "👩🏾‍👨🏾‍👧🏾‍👶🏾" })]
    [TestCase("👩🏾‍👨🏾‍👧🏾‍👶🏾 👩🏾‍👨🏾‍👧🏾‍👶🏾", 0, new[] { "👩🏾‍👨🏾‍👧🏾‍👶🏾", " ", "👩🏾‍👨🏾‍👧🏾‍👶🏾" })]
    [TestCase("a👩🏾‍👨🏾‍👧🏾‍👶🏾bc👩🏾‍👨🏾‍👧🏾‍👶🏾d", 0, new[] { "a", "👩🏾‍👨🏾‍👧🏾‍👶🏾", "bc", "👩🏾‍👨🏾‍👧🏾‍👶🏾", "d" })]
    public void ConsecutiveSurrogatePairsAndJoiner(string input, int _, string[] output)
    {
        BasicTest(input, output, new TestOptions { SurrogatePairBreakingOption = SurrogatePairBreakingOption.ConsecutiveSurrogatePairsAndJoiner });
    }

    [Test]
    public void Surrogates_1()
    {
        string str = new string(new[] { '\ud83d', '\udc69', '\ud83c', '\udffe' });
        string[] output = new[] { new string(new[] { '\ud83d', '\udc69', '\ud83c', '\udffe' }) };
        BasicTest(str, output, new TestOptions { SurrogatePairBreakingOption = SurrogatePairBreakingOption.ConsecutiveSurrogatePairsAndJoiner });
    }
    [Test]
    public void Surrogates_Incorrect1_High_no_Low()
    {
        string str = new string(new[] { '\ud83d', '\udc69', '\ud83c' });
        string[] output = new[] { new string(new[] { '\ud83d', '\udc69' }), new string(new char[] { '\ud83c' }) };
        BasicTest(str, output, new TestOptions { SurrogatePairBreakingOption = SurrogatePairBreakingOption.ConsecutiveSurrogatePairsAndJoiner });

    }
    [Test]
    public void Surrogates_Incorrect2_High_no_Low_Mixed()
    {
        string str = new string(new[] { '\ud83d', '\udc69', '\ud83c', 'a' });
        string[] output = new[] { new string(new[] { '\ud83d', '\udc69' }), new string(new char[] { '\ud83c' }), "a", };
        BasicTest(str, output, new TestOptions { SurrogatePairBreakingOption = SurrogatePairBreakingOption.ConsecutiveSurrogatePairsAndJoiner });
    }

    [Test]
    public void Surrogates_Incorrect3_Low_no_High()
    {
        string str = new string(new[] { '\udc69', '\ud83c', '\udffe' });
        string[] output = new[] { new string(new[] { '\udc69' }), new string(new char[] { '\ud83c', '\udffe' }) };
        BasicTest(str, output, new TestOptions { SurrogatePairBreakingOption = SurrogatePairBreakingOption.ConsecutiveSurrogatePairsAndJoiner });
    }






    [Test]
    [TestCase("A123", 0, new[] { "A", "123" })]
    public void BreakNumAfterText(string input, int _, string[] output)
    {

        BasicTest(input, output, new TestOptions { BreakNumberAfterText = true });
    }

    [Test]
    [TestCase("a.m", 0, new[] { "a.m" })]
    [TestCase("a.m.", 0, new[] { "a.m." })]
    [TestCase("a.m", 0, new[] { "a.m" })]
    [TestCase("9 a.m.", 0, new[] { "9", " ", "a.m." })]
    public void DontBreakPerioidInTextSpan(string input, int _, string[] output)
    {
        BasicTest(input, output, new TestOptions { BreakNumberAfterText = true });
    }
}
