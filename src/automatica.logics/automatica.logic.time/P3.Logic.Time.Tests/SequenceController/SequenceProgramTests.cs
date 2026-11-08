using System;
using System.Linq;
using P3.Logic.Time.SequenceController;
using Xunit;

namespace P3.Logic.Time.Tests.SequenceController
{
    public class SequenceProgramTests
    {
        [Fact]
        public void LinesBeforeTheFirstHeader_AreSequenceOne()
        {
            var program = SequenceProgram.Parse("set AQ1 = 1\nsequence Alarm\nset AQ2 = 2");

            Assert.Empty(program.Errors);
            Assert.Equal(2, program.Sequences.Count);
            Assert.Equal("1", program.Sequences[0].Name);
            Assert.Equal("Alarm", program.Sequences[1].Name);
            Assert.Equal(1, program.FindSequence("alarm"));
            Assert.Equal(1, program.FindSequence("2"));
            Assert.Null(program.FindSequence("3"));
        }

        [Fact]
        public void CommentsAndBlankLinesBeforeTheFirstHeader_DoNotCreateASequence()
        {
            var program = SequenceProgram.Parse("// heating\n\nsequence Heat\nset AQ1 = 1");
            Assert.Single(program.Sequences);
            Assert.Equal("Heat", program.Sequences[0].Name);
        }

        [Fact]
        public void LineNumbers_CountAllLinesOfTheSequence()
        {
            var program = SequenceProgram.Parse("sequence A\n// comment\n\nset AQ1 = 1\ngoto 1");

            var lines = program.Sequences[0].Lines;
            Assert.Equal(4, lines.Count);
            Assert.Equal(SequenceCommand.None, lines[0].Command);
            Assert.Equal(SequenceCommand.None, lines[1].Command);
            Assert.Equal(SequenceCommand.Set, lines[2].Command);
            Assert.Equal(SequenceCommand.Goto, lines[3].Command);
            Assert.Equal(1, lines[3].Target);
        }

        [Fact]
        public void Commands_AreCaseInsensitive()
        {
            var program = SequenceProgram.Parse("SET aq1 = AI1 * 2\nSleep 3 S\nWaitCondition ai2 > 1\nIF AQ1 >= 4\nSetPulse AQ2\nENDIF");
            Assert.Empty(program.Errors);
            Assert.Equal(new[] { SequenceCommand.Set, SequenceCommand.Sleep, SequenceCommand.Wait, SequenceCommand.If, SequenceCommand.SetPulse, SequenceCommand.EndIf },
                program.Sequences[0].Lines.Select(l => l.Command));
        }

        [Theory]
        [InlineData("sleep 300 s", 300_000)]
        [InlineData("sleep 300", 300_000)]
        [InlineData("sleep 10 m", 600_000)]
        [InlineData("sleep 1.5 h", 5_400_000)]
        [InlineData("sleep 250 ms", 250)]
        [InlineData("sleep AI1 * 2 s", 6_000)]
        [InlineData("sleep 2m", 120_000)]
        public void Sleep_Units(string line, double expectedMs)
        {
            var program = SequenceProgram.Parse(line);
            Assert.Empty(program.Errors);
            var values = new double[program.SlotCount];
            values[program.Resolve("AI1")!.Value] = 3;
            Assert.Equal(expectedMs, program.Sequences[0].Lines[0].Expression(values), 6);
        }

        [Fact]
        public void Variables_ComeFromAssignments_AndValue1To5AlwaysExist()
        {
            var program = SequenceProgram.Parse("set level = AI1\nset AQ1 = level + value3");
            Assert.Empty(program.Errors);
            Assert.Contains("level", program.Variables);
            Assert.Contains("value5", program.Variables);
        }

        [Fact]
        public void UnknownName_IsAnError_AndTheLineIsSkipped()
        {
            var program = SequenceProgram.Parse("set AQ1 = levle + 1\nset AQ2 = 2");
            var error = Assert.Single(program.Errors);
            Assert.Contains("levle", error);
            Assert.Contains("line 1", error);
            Assert.Equal(SequenceCommand.None, program.Sequences[0].Lines[0].Command);
            Assert.Equal(SequenceCommand.Set, program.Sequences[0].Lines[1].Command);
        }

        [Theory]
        [InlineData("jump 3", "unknown command")]
        [InlineData("set AQ9 = 1", "not an input, output or variable")]
        [InlineData("set AQ1", "expects")]
        [InlineData("goto x", "line number")]
        [InlineData("goto 5", "outside the sequence")]
        [InlineData("startsequence Nope", "does not exist")]
        [InlineData("if AI1 > 1", "without \"endif\"")]
        [InlineData("endif", "without \"if\"")]
        [InlineData("else", "without \"if\"")]
        [InlineData("return now", "no arguments")]
        [InlineData("set TQ = \"open", "closing")]
        [InlineData("setpulse TQ", "TQ")]
        [InlineData("set AQ1 = 1 +", "value expected")]
        [InlineData("set AQ1 = foo(1)", "unknown function")]
        public void InvalidLines_AreReported(string line, string expected)
        {
            var program = SequenceProgram.Parse(line);
            Assert.Contains(program.Errors, e => e.Contains(expected, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void MoreThanEightSequences_AreIgnored()
        {
            var text = string.Join("\n", Enumerable.Range(1, 9).Select(i => $"sequence S{i}\nset AQ1 = {i}"));
            var program = SequenceProgram.Parse(text);
            Assert.Equal(8, program.Sequences.Count);
            Assert.Contains(program.Errors, e => e.Contains("at most 8"));
        }

        [Fact]
        public void IfElseEndif_AreLinked()
        {
            var program = SequenceProgram.Parse("if AI1 > 1\nset AQ1 = 1\nelse\nset AQ1 = 2\nendif");
            Assert.Empty(program.Errors);
            var lines = program.Sequences[0].Lines;
            Assert.Equal(4, lines[0].Target);
            Assert.Equal(2, lines[0].ElseIndex);
            Assert.Equal(4, lines[2].Target);
        }

        [Fact]
        public void CommentSlashesInsideText_AreText()
        {
            var program = SequenceProgram.Parse("set TQ = \"http://x\" AQ1 // comment");
            Assert.Empty(program.Errors);
            var parts = program.Sequences[0].Lines[0].TextParts;
            Assert.Equal("http://x", parts[0].Text);
            Assert.NotNull(parts[1].Value);
        }

        [Theory]
        [InlineData("AI1 + 2 * 3", 11)]
        [InlineData("(AI1 + 2) * 3", 21)]
        [InlineData("2 ^ 3 ^ 2", 512)]
        [InlineData("AI1 > 4 and AI1 < 6", 1)]
        [InlineData("AI1 > 6 or not (AI1 == 5)", 0)]
        [InlineData("AI1 >= 5 && AI1 != 4", 1)]
        [InlineData("max(1, AI1, 3)", 5)]
        [InlineData("if(AI1 > 3, 10, 20)", 10)]
        [InlineData("round(2.345, 2)", 2.35)]
        [InlineData("-AI1 % 3", -2)]
        [InlineData("true + false", 1)]
        public void Expressions(string expression, double expected)
        {
            var program = SequenceProgram.Parse("set AQ1 = " + expression);
            Assert.Empty(program.Errors);
            var values = new double[program.SlotCount];
            values[program.Resolve("AI1")!.Value] = 5;
            Assert.Equal(expected, program.Sequences[0].Lines[0].Expression(values), 6);
        }

        [Fact]
        public void TooManyLines_AreCut()
        {
            var program = SequenceProgram.Parse(string.Join("\n", Enumerable.Repeat("set AQ1 = 1", SequenceProgram.MaxLines + 5)));
            Assert.Equal(SequenceProgram.MaxLines, program.Sequences[0].Lines.Count);
            Assert.Contains(program.Errors, e => e.Contains("more than"));
        }

        [Fact]
        public void EmptyProgram_HasNoSequences()
        {
            Assert.Empty(SequenceProgram.Parse(null).Sequences);
            Assert.Empty(SequenceProgram.Parse("  \n// nothing").Sequences);
        }
    }
}
