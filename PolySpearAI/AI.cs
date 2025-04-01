using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolySpearAI
{
    public class AI
    {
        private readonly HexGrid _grid;
        private const int MAX_DEPTH = 3;
        private const int UNIT_VALUE = 100;

        private const int MIN_VALUE = int.MinValue + 1;
        private const int MAX_VALUE = int.MaxValue - 1;

        public AI(HexGrid grid)
        {
            _grid = grid;
        }

        public async Task<(Hex From, Hex To)> FindBestMove()
        {
            Hex bestFrom = null;
            Hex bestTo = null;
            int bestScore = MIN_VALUE;
            int alpha = MIN_VALUE;
            int beta = MAX_VALUE;
            PLAYER aiPlayer = Program.CurrentPlayer;

            var playerUnits = GetPlayerUnits(aiPlayer);

            var tasks = new List<Task<MoveEvaluation>>();

            foreach (var unit in playerUnits)
            {
                var unitMovesTask = Task.Run(() =>
                {
                    var moves = _grid.AllowedMoves(unit);
                    MoveEvaluation eval = new MoveEvaluation { Score = MIN_VALUE };

                    foreach (var move in moves)
                    {
                        HexGrid simulatedGrid = SimulateGrid(_grid);
                        Hex simulatedTo = simulatedGrid.GetHex(move.Q, move.R);

                        simulatedGrid.MoveUnit(unit, simulatedTo);
                        int score = Minimax(simulatedGrid, MAX_DEPTH - 1, alpha, beta, false, aiPlayer);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestFrom = _grid.GetHex(unit);
                            bestTo = move;
                        }

                        alpha = Math.Max(alpha, bestScore);
                    }

                    return eval;
                });


                tasks.Add(unitMovesTask);
            }

            var results = await Task.WhenAll(tasks);

            foreach (var eval in results)
            {
                if (eval.Score > bestScore)
                {
                    bestScore = eval.Score;
                    bestFrom = eval.From;
                    bestTo = eval.To;
                }
            }

            return (bestFrom, bestTo);
        }

        private class MoveEvaluation
        {
            public int Score { get; set; }
            public Hex From { get; set; }
            public Hex To { get; set; }
        }

        private int Minimax(HexGrid grid, int depth, int alpha, int beta, bool maximizingPlayer, PLAYER aiPlayer)
        {
            PLAYER currentPlayer = maximizingPlayer ? aiPlayer : Program.GetEnemyPlayer(aiPlayer);

            // Base case: depth reached or game over
            if (depth == 0 || IsGameOver(grid))
            {
                return EvaluatePosition(grid, aiPlayer);
            }

            if (maximizingPlayer)
            {
                int maxEval = MIN_VALUE;
                var playerUnits = GetPlayerUnits(currentPlayer, grid);

                foreach (var unit in playerUnits)
                {
                    var moves = grid.AllowedMoves(unit);

                    foreach (var move in moves)
                    {
                        HexGrid simulatedGrid = SimulateGrid(grid);
                        Hex simulatedTo = simulatedGrid.GetHex(move.Q, move.R);

                        simulatedGrid.MoveUnit(unit, simulatedTo);

                        int eval = Minimax(simulatedGrid, depth - 1, alpha, beta, false, aiPlayer);
                        maxEval = Math.Max(maxEval, eval);
                        alpha = Math.Max(alpha, eval);

                        if (beta <= alpha)
                            break; // Alpha-beta pruning
                    }
                }

                return maxEval == MIN_VALUE ? EvaluatePosition(grid, aiPlayer) : maxEval;
            }
            else
            {
                int minEval = MAX_VALUE;
                var playerUnits = GetPlayerUnits(currentPlayer, grid);

                foreach (var unit in playerUnits)
                {
                    var moves = grid.AllowedMoves(unit);

                    foreach (var move in moves)
                    {
                        HexGrid simulatedGrid = SimulateGrid(grid);
                        Hex simulatedFrom = simulatedGrid.GetHex(unit);
                        Hex simulatedTo = simulatedGrid.GetHex(move.Q, move.R);

                        simulatedGrid.MoveUnit(unit, simulatedTo);

                        int eval = Minimax(simulatedGrid, depth - 1, alpha, beta, true, aiPlayer);
                        minEval = Math.Min(minEval, eval);
                        beta = Math.Min(beta, eval);

                        if (beta <= alpha)
                            break; // Alpha-beta pruning
                    }
                }

                return minEval == MAX_VALUE ? EvaluatePosition(grid, aiPlayer) : minEval;
            }
        }

        private List<Unit> GetPlayerUnits(PLAYER player, HexGrid grid = null)
        {
            grid = grid ?? _grid;
            List<Unit> units = grid.GetUnitsByPlayer(player).ToList();

            return units;
        }

        private bool IsGameOver(HexGrid grid)
        {
            bool player0HasUnits = grid.GetUnitsByPlayer(PLAYER.ELF).Any();
            bool player1HasUnits = grid.GetUnitsByPlayer(PLAYER.ORC).Any();

            return !player0HasUnits || !player1HasUnits;
        }

        private int EvaluatePosition(HexGrid grid, PLAYER aiPlayer)
        {
            int score = 0;
            PLAYER enemyPlayer = Program.GetEnemyPlayer(aiPlayer);

            int aiUnitCount = grid.GetUnitsByPlayer(aiPlayer).Count;
            int enemyUnitCount = grid.GetUnitsByPlayer(enemyPlayer).Count;

            score += (aiUnitCount - enemyUnitCount) * UNIT_VALUE * 2;

            if (enemyUnitCount == 0)
                return MAX_VALUE;
            if (aiUnitCount == 0)
                return MIN_VALUE;

            return score;
        }

        private HexGrid SimulateGrid(HexGrid original)
        {
            return null;
        }
    }
}
