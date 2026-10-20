using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChiefComplaint.Sim.Bots;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;

namespace ChiefComplaint.Sim.Stats
{
    public static class Runner
    {
        /// <summary>Guards against a bot that never ends its shift.</summary>
        public const int MaxStepsPerShift = 400;

        public static RunRecord RunOne(GameData data, GameConfig config, BotFactory botFactory, int seed, IRunObserver extraObserver = null)
        {
            var recorder = new RunRecorder();
            IRunObserver observer = extraObserver == null ? (IRunObserver)recorder : new CompositeObserver(recorder, extraObserver);
            var game = new Game(data, seed, observer, config);
            IBot bot = botFactory(unchecked(seed * 31 + 17));
            game.Start();

            int steps = 0, stepsThisShift = 0, shift = game.Shift;
            while (!game.IsOver)
            {
                List<GameAction> legal = game.LegalActions();
                GameAction action = bot.Choose(game, legal);
                if (game.Phase == Phase.Shift && ++stepsThisShift > MaxStepsPerShift) action = GameAction.EndShift;
                game.Apply(action);
                steps++;
                if (game.Shift != shift) { shift = game.Shift; stepsThisShift = 0; }
            }
            recorder.Record.Seed = seed;
            recorder.Record.Steps = steps;
            return recorder.Record;
        }

        /// <summary>Plays <paramref name="runs"/> seeded runs in parallel. Run i uses seed <c>baseSeed + i</c>, so results are reproducible.</summary>
        public static RunRecord[] RunBatch(GameData data, GameConfig config, BotFactory botFactory, int runs, int baseSeed)
        {
            var records = new RunRecord[runs];
            Parallel.For(0, runs, i => records[i] = RunOne(data, config, botFactory, baseSeed + i));
            return records;
        }
    }
}
