using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PolySpearAI.Unit;

namespace PolySpearAI
{
    public class AI
    {
        private readonly HexGrid _grid;
        private const int MAX_DEPTH = 3;
        private const int UNIT_VALUE = 100;

        private const int INF = 10_000_000;
        private const int WIN_SCORE = 1_000_000;

        public AI(HexGrid grid)
        {
            _grid = grid;
        }

        public (Hex From, Hex To) FindBestMove(PLAYER aiPlayer)
        {
            Hex bestFrom = null;
            Hex bestTo = null;
            int bestScore = -INF;

            List<Unit> playerUnits = GetPlayerUnits(aiPlayer);

            foreach (var unit in playerUnits)
            {
                Hex currentPos = _grid.GetHex(unit);
                List<Hex> moves = _grid.AllowedMoves(unit);
                foreach (var move in moves)
                {
                    PreMove previousMove = new PreMove(_grid);
                    Hex simulatedTo = _grid.GetHex(move.Q, move.R);
                    if(!_grid.MoveUnit(unit, simulatedTo))
                    {
                        continue;
                    }
                    // Call Negamax instead of Minimax
                    int score = -Negamax(MAX_DEPTH - 1, -INF, INF, Program.GetEnemyPlayer(aiPlayer), -1, aiPlayer);
                    // Revert the move.
                    _grid.ApplyMove(previousMove);
                    // If this move yields a better score, update our best move.
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestFrom = currentPos;
                        bestTo = move;
                    }
                }
            }

            return (bestFrom, bestTo);
        }

        private int Negamax(int depth, int alpha, int beta, PLAYER currentPlayer, int color, PLAYER aiPlayer)
        {
            if (depth == 0 || _grid.IsGameOver())
            {
                return color * EvaluatePosition(currentPlayer);
            }

            int value = -INF;
            foreach (var unit in GetPlayerUnits(currentPlayer))
            {
                foreach (var move in _grid.AllowedMoves(unit))
                {
                    PreMove previousMove = new PreMove(_grid);
                    Hex simulatedTo = _grid.GetHex(move.Q, move.R);
                    _grid.MoveUnit(unit, simulatedTo);

                    // Recursively call Negamax with inverted alpha/beta and color
                    int eval = -Negamax(depth - 1, -beta, -alpha, Program.GetEnemyPlayer(currentPlayer), -color, aiPlayer);

                    _grid.ApplyMove(previousMove);
                    value = Math.Max(value, eval);
                    alpha = Math.Max(alpha, eval);
                    if (alpha >= beta)
                    {
                        break; // Alpha-beta pruning
                    }
                }
            }
            return value;
        }

        private List<Unit> GetPlayerUnits(PLAYER player)
        {
            return _grid.GetUnitsByPlayer(player).ToList();
        }

        private int EvaluatePosition(PLAYER aiPlayer)
        {
            int score = 0;
            PLAYER enemyPlayer = Program.GetEnemyPlayer(aiPlayer);

            int aiUnitCount = _grid.GetUnitsByPlayer(aiPlayer).Count;
            int enemyUnitCount = _grid.GetUnitsByPlayer(enemyPlayer).Count;

            score += (aiUnitCount - enemyUnitCount) * UNIT_VALUE;

            if (enemyUnitCount == 0)
                return WIN_SCORE;
            if (aiUnitCount == 0)
                return -INF;

            return score;
        }
    }
}
