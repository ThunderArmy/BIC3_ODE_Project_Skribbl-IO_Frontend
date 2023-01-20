using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDrawing
{

    public class GameState
    {
        public GameState()
        {
        }

        public GameState(string currentWord, int remainingTime, GameStatusEnum status, List<Player> players, Player currentDrawingPlayer, int currentRound, int maxRounds)
        {
            CurrentWord = currentWord;
            RemainingTime = remainingTime;
            Status = status;
            Players = players;
            CurrentDrawingPlayer = currentDrawingPlayer;
            this.currentRound = currentRound;
            this.maxRounds = maxRounds;
        }

        public string CurrentWord { get; set; } = string.Empty;
        public int RemainingTime { get; set; }
        public GameStatusEnum Status { get; set; } = GameStatusEnum.WaitingForPlayers;
        public List<Player> Players { get; set; } = new List<Player>();
        public Player CurrentDrawingPlayer { get; } = null;
        public int currentRound { get; set; }
        public int maxRounds { get; set; } = 3;
        public Player OwnPlayer { get; set; }

        public event EventHandler<bool> startGame; 

        public bool JoinGame (Player player)
        {
            if(Status == GameStatusEnum.WaitingForPlayers) {
                Players.Add(player);
                return true;
            }
            return false;
        }

    }
}
