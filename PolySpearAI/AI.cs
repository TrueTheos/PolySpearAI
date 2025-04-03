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
        private const int MAX_DEPTH = 10;
        private const int UNIT_VALUE = 100;

        private const int INF = 10_000_000;
        private const int WIN_SCORE = 1_000_000;

        private PLAYER _aiPlayer;

        private Hex _moveFrom;
        private Hex _bestTarget;
        private int _expectedEval;

        public AI(HexGrid grid)
        {
            _grid = grid;
        }

        public struct BestMove
        {
            public Hex From;
            public Hex To;
            public int CurrentEval;
            public int EvalAfterMove;

            public BestMove(Hex from, Hex to, int currentEval, int evalAfterMove)
            {
                From = from;
                To = to;
                CurrentEval = currentEval;
                EvalAfterMove = evalAfterMove;
            }
        }

        public BestMove FindBestMove(PLAYER aiPlayer)
        {
            _aiPlayer = aiPlayer;
            _moveFrom = null;
            _bestTarget = null;

            Negamax(MAX_DEPTH, 0, -INF, INF);

            Program.CurrentPlayer = _aiPlayer;

            return new BestMove(_moveFrom, _bestTarget, EvaluatePosition(_aiPlayer), _expectedEval);
        }

        private int Negamax(int depth, int ply_from_root, int alpha = -INF, int beta = INF)
        {
            if (depth == 0 || _grid.IsGameOver())
            {
                return EvaluatePosition(Program.CurrentPlayer);
            }

            Hex moveOrigin = null;
            Hex bestTarget = null;
            int bestScore = -INF;

            List<Unit> units = GetPlayerUnits(Program.CurrentPlayer);
            foreach (var unit in units)
            {
                Hex currentPos = _grid.GetHex(unit);
                List<Hex> allowedMoves = _grid.AllowedMoves(unit);
                foreach (var move in allowedMoves)
                {
                    BoardState preMoveBoardState = new BoardState(_grid);
                    PLAYER prePlayer = Program.CurrentPlayer;
                    Hex targetHex = _grid.GetHex(move.Q, move.R);
                    _grid.MoveUnit(unit, targetHex);

                    Program.ChangePlayer();

                    int eval = -Negamax(depth - 1, ply_from_root + 1, -beta, -alpha);

                    _grid.SetBoardState(preMoveBoardState);

                    Program.CurrentPlayer = prePlayer;

                    if (eval > bestScore)
                    {
                        bestScore = eval;
                        moveOrigin = currentPos;
                        bestTarget = targetHex;
                    }

                    alpha = Math.Max(alpha, eval);
                    if (alpha >= beta)
                        break; 
                }

                if (alpha >= beta)
                    break;
            }

            if (ply_from_root == 0)
            {
                _moveFrom = moveOrigin;
                _bestTarget = bestTarget;
                _expectedEval = bestScore;
            }

            return bestScore;
        }

        private List<Unit> GetPlayerUnits(PLAYER player)
        {
            return _grid.GetUnitsByPlayer(player).ToList();
        }

        private int EvaluatePosition(PLAYER currentPlayer)
        {
            int score = 0;
            PLAYER enemyPlayer = Program.GetEnemyPlayer(currentPlayer);

            int aiUnitCount = _grid.GetUnitsByPlayer(currentPlayer).Count;
            int enemyUnitCount = _grid.GetUnitsByPlayer(enemyPlayer).Count;

            score += (aiUnitCount - enemyUnitCount) * UNIT_VALUE;

            if (enemyUnitCount == 0)
                return WIN_SCORE;
            if (aiUnitCount == 0)
                return -WIN_SCORE;

            return score;
        }
    }
}
