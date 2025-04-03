using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PolySpearAI.Unit;
using static System.Formats.Asn1.AsnWriter;

namespace PolySpearAI
{
    public class AI
    {
        private readonly HexGrid _grid;
        private const int MAX_DEPTH = 3;
        private const int UNIT_VALUE = 100;

        private const int INF = 10_000_000;
        private const int WIN_SCORE = 1_000_000;

        private PLAYER _aiPlayer;

        private Hex _moveFrom;
        private Hex _bestTarget;

        public AI(HexGrid grid)
        {
            _grid = grid;
        }

        public (Hex From, Hex To) FindBestMove(PLAYER aiPlayer)
        {
            _aiPlayer = aiPlayer;
            _moveFrom = null;
            _bestTarget = null;

            Negamax(MAX_DEPTH, aiPlayer, 1, 0);

            return (_moveFrom, _bestTarget);
        }

        private int Negamax(int depth, PLAYER aiPlayer, int color, int ply_from_root)
        {
            if (depth == 0 || _grid.IsGameOver())
            {
                return color * EvaluatePosition();
            }

            Hex moveOrigin = null;
            Hex bestTarget = null;
            int bestScore = -INF;

            foreach (var unit in GetPlayerUnits(aiPlayer))
            {
                Hex currentPos = _grid.GetHex(unit);
                foreach (var move in _grid.AllowedMoves(unit))
                {
                    BoardState preMoveBoardState = new BoardState(_grid);
                    Hex targetHex = _grid.GetHex(move.Q, move.R);
                    _grid.MoveUnit(unit, targetHex);

                    // Recursively call Negamax with inverted alpha/beta and color
                    int eval = -Negamax(depth - 1, Program.GetEnemyPlayer(aiPlayer), -color, ply_from_root + 1);

                    _grid.SetBoardState(preMoveBoardState);                  

                    if(eval > bestScore)
                    {
                        bestScore = eval;
                        moveOrigin = currentPos;
                        bestTarget = targetHex;
                    }
                }
            }

            if (ply_from_root == 0)
            {
                _moveFrom = moveOrigin;
                _bestTarget = bestTarget;
            }

            return bestScore;
        }

        private List<Unit> GetPlayerUnits(PLAYER player)
        {
            return _grid.GetUnitsByPlayer(player).ToList();
        }

        private int EvaluatePosition()
        {
            int score = 0;
            PLAYER enemyPlayer = Program.GetEnemyPlayer(_aiPlayer);

            int aiUnitCount = _grid.GetUnitsByPlayer(_aiPlayer).Count;
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
