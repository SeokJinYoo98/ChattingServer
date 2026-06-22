using Yujanggi.Core.Domain;

namespace Yujanggi.Core.Board
{
    internal class BoardInitializer
    {
        public  static void SetUpPieces(IBoardModel board, Formation cho, Formation han)
        {
            int pieceId = 0;
            SpawnCho(board, PlayerTeam.Cho, ref pieceId);
            SpawnHan(board, PlayerTeam.Han, ref pieceId);

            ApplyFormationCho(board, cho);
            ApplyFormationHan(board, han);
        }
        private static void SpawnCho(IBoardModel board, PlayerTeam cho, ref int pieceId)
        {
            Spawn(board, cho, PieceType.Chariot, 0, 0, ref pieceId);
            Spawn(board, cho, PieceType.Chariot, 8, 0, ref pieceId);

            Spawn(board, cho, PieceType.Elephant, 1, 0, ref pieceId);
            Spawn(board, cho, PieceType.Elephant, 7, 0, ref pieceId);

            Spawn(board, cho, PieceType.Horse, 6, 0, ref pieceId);
            Spawn(board, cho, PieceType.Horse, 2, 0, ref pieceId);

            Spawn(board, cho, PieceType.Guard, 3, 0, ref pieceId);
            Spawn(board, cho, PieceType.Guard, 5, 0, ref pieceId);

            Spawn(board, cho, PieceType.King, 4, 1, ref pieceId);

            Spawn(board, cho, PieceType.Cannon, 1, 2, ref pieceId);
            Spawn(board, cho, PieceType.Cannon, 7, 2, ref pieceId);

            Spawn(board, cho, PieceType.Soldier, 0, 3, ref pieceId);
            Spawn(board, cho, PieceType.Soldier, 2, 3, ref pieceId);
            Spawn(board, cho, PieceType.Soldier, 4, 3, ref pieceId);
            Spawn(board, cho, PieceType.Soldier, 6, 3, ref pieceId);
            Spawn(board, cho, PieceType.Soldier, 8, 3, ref pieceId);
        }
        private static void SpawnHan(IBoardModel board, PlayerTeam han, ref int pieceId)
        {
            Spawn(board, han, PieceType.Chariot, 0, 9, ref pieceId);
            Spawn(board, han, PieceType.Chariot, 8, 9, ref pieceId);

            Spawn(board, han, PieceType.Elephant, 1, 9, ref pieceId);
            Spawn(board, han, PieceType.Elephant, 7, 9, ref pieceId);

            Spawn(board, han, PieceType.Horse, 6, 9, ref pieceId);
            Spawn(board, han, PieceType.Horse, 2, 9, ref pieceId);

            Spawn(board, han, PieceType.Guard, 3, 9, ref pieceId);
            Spawn(board, han, PieceType.Guard, 5, 9, ref pieceId);

            Spawn(board, han, PieceType.King, 4, 8, ref pieceId);

            Spawn(board, han, PieceType.Cannon, 1, 7, ref pieceId);
            Spawn(board, han, PieceType.Cannon, 7, 7, ref pieceId);

            Spawn(board, han, PieceType.Soldier, 0, 6, ref pieceId);
            Spawn(board, han, PieceType.Soldier, 2, 6, ref pieceId);
            Spawn(board, han, PieceType.Soldier, 4, 6, ref pieceId);
            Spawn(board, han, PieceType.Soldier, 6, 6, ref pieceId);
            Spawn(board, han, PieceType.Soldier, 8, 6, ref pieceId);
        }  
        private static void Spawn(IBoardModel board, PlayerTeam team, PieceType type, int x, int z, ref int pieceId)
            => board.SetPiece(new Pos(x, z), new PieceModel(type, team, pieceId++));
        private static void ApplyFormationCho(IBoardModel board, Formation cho)
        {
            if (cho == Formation.EHHE) return;

            Pos left_1  = new Pos(1, 0);
            Pos left_2  = new Pos(2, 0);
            Pos right_1 = new Pos(6, 0);
            Pos right_2 = new Pos(7, 0);

            if (cho == Formation.EHEH)
            {
                var sawp1 = board.GetPiece(right_1);
                var swap2 = board.GetPiece(right_2);
                board.SetPiece(right_1, swap2);
                board.SetPiece(right_2, sawp1);
            }
            else if (cho == Formation.HEEH)
            {
                var sawp1 = board.GetPiece(right_1);
                var swap2 = board.GetPiece(right_2);
                board.SetPiece(right_1, swap2);
                board.SetPiece(right_2, sawp1);

                sawp1 = board.GetPiece(left_1);
                swap2 = board.GetPiece(left_2);
                board.SetPiece(left_1, swap2);
                board.SetPiece(left_2, sawp1);
            }
            else if (cho == Formation.HEHE)
            {
                var swap1 = board.GetPiece(left_1);
                var swap2 = board.GetPiece(left_2);
                board.SetPiece(left_1, swap2);
                board.SetPiece(left_2, swap1);
            }
        }
        private static void ApplyFormationHan(IBoardModel board, Formation han)
        {
            if (han == Formation.EHHE) return;

            Pos left_1  = new Pos(1, 9);
            Pos left_2  = new Pos(2, 9);
            Pos right_1 = new Pos(6, 9);
            Pos right_2 = new Pos(7, 9);

            if (han == Formation.EHEH)
            {
                var sawp1 = board.GetPiece(left_1);
                var swap2 = board.GetPiece(left_2);
                board.SetPiece(left_1, swap2);
                board.SetPiece(left_2, sawp1);
            }
            else if (han == Formation.HEEH)
            {
                var sawp1 = board.GetPiece(left_1);
                var swap2 = board.GetPiece(left_2);
                board.SetPiece(left_1, swap2);
                board.SetPiece(left_2, sawp1);

                sawp1 = board.GetPiece(right_1);
                swap2 = board.GetPiece(right_2);
                board.SetPiece(right_1, swap2);
                board.SetPiece(right_2, sawp1);
            }
            else if (han == Formation.HEHE)
            {
                var swap1 = board.GetPiece(right_1);
                var swap2 = board.GetPiece(right_2);
                board.SetPiece(right_1, swap2);
                board.SetPiece(right_2, swap1);
            }
        }
    }
}