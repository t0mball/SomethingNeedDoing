﻿using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;

namespace SomethingNeedDoing.Grammar.Commands
{
    /// <summary>
    /// The /loop command.
    /// </summary>
    internal class LoopCommand : MacroCommand
    {
        private const int MaxLoops = int.MaxValue;
        private static readonly Regex Regex = new(@"^/loop(?:\s+(?<count>\d+))?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly bool echo;
        private int loopsRemaining;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="loopCount">Loop count.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="echo">Echo value.</param>
        private LoopCommand(string text, int loopCount, WaitModifier wait, EchoModifier echo)
            : base(text, wait)
        {
            this.loopsRemaining = loopCount >= 0 ? loopCount : MaxLoops;
            this.echo = echo.PerformEcho;
        }

        /// <summary>
        /// Parse the text as a command.
        /// </summary>
        /// <param name="text">Text to parse.</param>
        /// <returns>A parsed command.</returns>
        public static LoopCommand Parse(string text)
        {
            _ = WaitModifier.TryParse(ref text, out var waitModifier);
            _ = EchoModifier.TryParse(ref text, out var echoModifier);

            var match = Regex.Match(text);
            if (!match.Success)
                throw new MacroSyntaxError(text);

            var countGroup = match.Groups["count"];
            var countValue = countGroup.Success
                ? int.Parse(countGroup.Value, CultureInfo.InvariantCulture)
                : int.MaxValue;

            return new LoopCommand(text, countValue, waitModifier, echoModifier);
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            if (this.loopsRemaining != MaxLoops)
            {
                if (this.echo && this.loopsRemaining >= 0)
                {
                    var noun = this.loopsRemaining == 1 ? "loop" : "loops";
                    Service.ChatManager.PrintMessage($"{this.loopsRemaining} {noun} remaining");
                }

                this.loopsRemaining--;
                if (this.loopsRemaining <= 0)
                    return;
            }

            Service.MacroManager.Loop();

            await this.PerformWait(token);

            Service.MacroManager.LoopCheckForPause();
        }
    }
}