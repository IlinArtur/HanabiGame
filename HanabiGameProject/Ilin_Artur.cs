using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace HanabiGameProject
{
    class Ilin_Artur
    {
        static void Main(string[] args)
        {
            HanabiGame game = new HanabiGame();
            GameContoller controller = new GameContoller(game);
            if (args.Length > 0)
                controller.GameLoop(new StreamReader(args[0]));
            else
                controller.GameLoop(new StreamReader(Console.OpenStandardInput(), false));
        }
    }

    class HanabiGameException : Exception
    {
        public HanabiGameException(string message) : base(message) { }
    }

    class InvalidTurnException : HanabiGameException
    {
        public InvalidTurnException(string message) : base(message) { }
    }

    public class HanabiGame
    {
        private enum GameState
        {
            Ended,
            Started
        }

        private class CardInfo
        {
            public CardColor ColorInfo { get; set; }
            public CardRank RankInfo { get; set; }

        }

        private int score = 0;
        //private int turns = 0;
        private int risks = 0;
        private int succCards = 0;
        private Queue<Card> deck;

        public int Score => score;
        //public int Turns => turns;
        public int Risks => risks;
        public int SuccCards => succCards;
        public bool Finished => gameState == GameState.Ended;
        public Table CardTable => table;

        private GameState gameState;
        private Table table = new Table();
        private IDictionary<Card, CardInfo> cardsInfo = new Dictionary<Card, CardInfo>();

        public const int HAND_SIZE = 5;
        public const int MIN_PLAYERS = 2;

        private bool validateCardPosition(int cardPosition)
        {
            return (cardPosition < HAND_SIZE) && (cardPosition >= 0);
        }

        private void IncrementTurns()
        {
            //turns++;
        }
        
        public void EndGame()
        {
            gameState = GameState.Ended;
        }

        public HanabiGame()
        {
            gameState = GameState.Ended;
        }


        public void StartGame(IEnumerable<Card> deck, IEnumerable<Player> players, int difficulty)
        {
            if (deck == null) throw new ArgumentNullException("deck");
            if (players == null) throw new ArgumentNullException("players");

            int minDeckSize = (players.Count() * HAND_SIZE) + 1;
            if (deck.Count() < minDeckSize) throw new HanabiGameException("Not Enough Cards");

            gameState = GameState.Started;
            //turns = 0;
            score = 0;
            risks = 0;
            succCards = 0;
            table.Clear();
            cardsInfo.Clear();
            this.deck = new Queue<Card>(deck);
            if (difficulty <= 0)
            {
                foreach (var card in this.deck)
                {
                    storeCardInfo(card, card.Color, card.Rank);
                }
            }
            dealCards(players);
        }

        private void dealCards(IEnumerable<Player> players)
        {
            foreach (var player in players)
            {
                for (int cardIndex = 0; cardIndex < HAND_SIZE; cardIndex++)
                {
                    Card nextCard = PickCardFromDeck();
                    player.AddToHand(nextCard);
                }
            }
        }

        public Card PickCardFromDeck()
        {
            bool deckEmpty = (deck == null) || (deck.Count == 0);
            if (deckEmpty) throw new HanabiGameException("Deck is Empty. Start new game.");
            if (deck.Count == 1) EndGame();
            return deck.Dequeue();
        }

        public void PlayCard(Card card)
        {
            if (table.TryPutCard(card))
            {
                score++;
                succCards++;
                CardInfo cardInfo;
                if (cardsInfo.TryGetValue(card, out cardInfo))
                {
                    var hasInfoOnCard = (cardInfo.ColorInfo != CardColor.Unknown) && (cardInfo.RankInfo != CardRank.Unknown);
                    if (!hasInfoOnCard)
                        risks++;
                } else
                {
                    risks++;
                }
            }
            else
            {
                throw new InvalidTurnException("Cannot play card " + card.ToString());
            }
        }
        
        private void storeCardInfo(Card playerCard, CardColor color, CardRank rank)
        {
            if (color != CardColor.Unknown)
            {
                if (playerCard.Color != color)
                    throw new InvalidTurnException(String.Format("Card color {0} not equals {1}", playerCard.ToString(), color));
            }
            if (rank != CardRank.Unknown)
            {
                if (playerCard.Rank != rank)
                {
                    throw new InvalidTurnException(String.Format("Card rank {0} not equals {1}", playerCard.ToString(), rank));
                }
            }

            CardInfo cardInfo;
            if (cardsInfo.TryGetValue(playerCard, out cardInfo))
            {
                if (color != CardColor.Unknown) cardInfo.ColorInfo = color;
                if (rank != CardRank.Unknown) cardInfo.RankInfo = rank;
            } else
            {
                cardInfo = new CardInfo();
                cardInfo.RankInfo = rank;
                cardInfo.ColorInfo = color;
                cardsInfo.Add(new KeyValuePair<Card, CardInfo>(playerCard, cardInfo));
            }            
        }

        public void TellCardColor(Card playerCard, CardColor color)
        {
            if (color == CardColor.Unknown) throw new InvalidTurnException("Color was not provided");
            storeCardInfo(playerCard, color, CardRank.Unknown);
        }

        public void TellCardRank(Card playerCard, CardRank rank)
        {
            if (rank == CardRank.Unknown) throw new InvalidTurnException("Rank was not provided");
            storeCardInfo(playerCard, CardColor.Unknown, rank);
        }

        public void DropCard(Card card)
        {
            //do something with dropped card
        }
    }

    public class Hand
    {
        private IList<Card> cards = new List<Card>(HanabiGame.HAND_SIZE);

        public IEnumerable<Card> Cards => cards;

        public Card ThrowCard(int removeCardPosition, Card newCard)
        {
            Card removedCard = cards.ElementAt(removeCardPosition);
            cards.RemoveAt(removeCardPosition);
            cards.Add(newCard);
            return removedCard;
        }
        
        public void Add(Card card)
        {
            cards.Add(card);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class Player
    {
        private Hand hand = new Hand();

        public IEnumerable<Card> Cards => hand.Cards;

        public Card DropCard(int cardPosition, Card newCard)
        {
            Card card = hand.ThrowCard(cardPosition, newCard);
            return card;
        }   
        
        public void AddToHand(Card card)
        {
            hand.Add(card);
        }

        public override string ToString()
        {
            string result = "";
            foreach (var card in Cards)
            {
                result += card.ToString() + " ";
            }
            return result.Trim();
        }
    }

    public class GameContoller
    {
        private const string HANABI_DIFFICULTY_PARAM = "HANABI_DIFICULTY_LEVEL";
        private const int DEFAULT_DIFFICULTY = 0;
        private Regex START_GAME_COMMAND = new Regex(@"Start new game with deck(\s[RGBWY][1-5]){11,}");
        private Regex PLAY_CARD_COMMAND = new Regex(@"Play card ([0-4])");
        private Regex TELL_RANK_COMMAND = new Regex(@"Tell rank (\d) for cards(\s\d)+");
        private Regex TELL_COLOR_COMMAND = new Regex(@"Tell color ([RGBWY])\w+ for cards(\s\d)+");
        private Regex DROP_CARD_COMMAND = new Regex(@"Drop card ([0-4])");

        private HanabiGame game;
        private Queue<Player> players = new Queue<Player>(5);
        private Player currentPlayer;
        private int turns = 0;

        private string[] _commandsList = new string[]
        {
            "Start new game with deck {card1} {card2}...",
            "Play card {card position}",
            "Tell {rank|color} {value} for cards {card position 1} {card position 2} ...",
            "Drop card {card position 1}",
            "  card position is a number in [0..4]",
            "  color any of R[ed] G[reen] B[lue] W[hite] Y[ellow]",
            "  rank is a number in [1..5]",
            "  card is a string {color}{rank}",
        };

        private void startGame(IEnumerable<string> cards)
        {
            IEnumerable<Card> deck = createDeckFromString(cards);
            var difficultyParam = Environment.GetEnvironmentVariable(HANABI_DIFFICULTY_PARAM);
            int difficulty;
            if (!int.TryParse(difficultyParam, out difficulty))
                difficulty = DEFAULT_DIFFICULTY;
            players.Clear();
            players.Enqueue(new Player());
            players.Enqueue(new Player());
            turns = 0;
            game.StartGame(deck, players, difficulty);
            currentPlayer = players.Dequeue();
        }

        private IEnumerable<Card> createDeckFromString(IEnumerable<string> cards)
        {
            foreach (var cardString in cards)
            {
                Card card = createCardFromString(cardString);
                if (card == null) continue;
                yield return card;
            }
        }

        private void dealCards(Player player)
        {
            for (int i = 0; i < HanabiGame.HAND_SIZE; i++)
            {
                Card nextCard = game.PickCardFromDeck();
                player.AddToHand(nextCard);
            }
        }

        private Card createCardFromString(string cardString)
        {
            CardColor color;
            string card = cardString.Trim();
            string inputRank = card.Substring(0, 1).ToUpper();
            if (!Enum.TryParse(inputRank, out color))
                return null;
            int rank = 0;
            string rankString = card.Substring(1, 1);
            if (!int.TryParse(rankString, out rank))
                return null;  
            CardRank cardRank = (CardRank)rank;
            return new Card(color, cardRank);
        }

        private Player nextPlayer()
        {
            return players.Peek();
        }

        private void moveNextPlayer()
        {
            turns++;
            players.Enqueue(currentPlayer);
            currentPlayer = players.Dequeue();
        }

        private void playCard(string cardPosition)
        {
            if (game.Finished) return;
            int position = int.Parse(cardPosition);
            Card newCard = game.PickCardFromDeck();
            Card playerCard = currentPlayer.DropCard(position, newCard);
            game.PlayCard(playerCard);
        }

        private void tellCardRank(string rankString, IEnumerable<int> cardsPositions)
        {
            int rank;
            if (!int.TryParse(rankString, out rank))
                return;
            CardRank cardRank = (CardRank)rank;
            foreach (var position in cardsPositions)
            {
                Card playerCard = nextPlayer().Cards.ElementAt(position);
                game.TellCardRank(playerCard, cardRank);
            }
        }

        private void tellCardColor(string colorString, IEnumerable<int> cardsPositions)
        {
            CardColor color;
            if (!Enum.TryParse(colorString, out color)) return;
            foreach (var position in cardsPositions)
            {
                Card playerCard = nextPlayer().Cards.ElementAt(position);
                game.TellCardColor(playerCard, color);
            }
        }

        private void dropCard(string cardPosition)
        {
            if (game.Finished) return;
            int position = int.Parse(cardPosition);
            Card newCard = game.PickCardFromDeck();
            Card playerCard = currentPlayer.DropCard(position, newCard);
            game.DropCard(playerCard);
        }

        public GameContoller(HanabiGame game)
        {
            this.game = game;
        }

        public void GameLoop(StreamReader reader)
        {
            if (reader == null) reader = new StreamReader(Console.OpenStandardInput(), false);
            var userInput = reader.ReadLine();
            if (userInput == "")
                userInput = null;
            while (userInput != null)
            {
                try
                {
                    Console.WriteLine("> " + userInput);
                    if (START_GAME_COMMAND.IsMatch(userInput))
                    {
                        Match match = START_GAME_COMMAND.Match(userInput);
                        IEnumerable<string> cards = match.Groups[1].Captures.Cast<Capture>().Select(c => c.Value);
                        startGame(cards);
                    }
                    else if (PLAY_CARD_COMMAND.IsMatch(userInput))
                    {
                        Match match = PLAY_CARD_COMMAND.Match(userInput);
                        string cardPosition = match.Groups[1].Value;
                        playCard(cardPosition);
                        moveNextPlayer();
                    }
                    else if (TELL_COLOR_COMMAND.IsMatch(userInput))
                    {
                        Match match = TELL_COLOR_COMMAND.Match(userInput);
                        string color = match.Groups[1].Value;
                        IEnumerable<int> cardPositions = match.Groups[2].Captures.Cast<Capture>().Select(c => int.Parse(c.Value));
                        tellCardColor(color, cardPositions);
                        moveNextPlayer();
                    }
                    else if (TELL_RANK_COMMAND.IsMatch(userInput))
                    {
                        Match match = TELL_RANK_COMMAND.Match(userInput);
                        string rank = match.Groups[1].Value;
                        IEnumerable<int> cardPositions = match.Groups[2].Captures.Cast<Capture>().Select(c => int.Parse(c.Value));
                        tellCardRank(rank, cardPositions);
                        moveNextPlayer();
                    }
                    else if (DROP_CARD_COMMAND.IsMatch(userInput))
                    {
                        Match match = DROP_CARD_COMMAND.Match(userInput);
                        string cardPosition = match.Groups[1].Value;
                        dropCard(cardPosition);
                        moveNextPlayer();
                    }
                    else
                    {
                        printCommandsList();
                    }
                    printGameInfo();
                }
                catch (HanabiGameException e)
                {
                    game.EndGame();
                    moveNextPlayer();
                    printGameInfo();
                }
                catch (Exception e)
                {
                    Console.WriteLine("There is error! Please check your input data and try again");
                    Console.WriteLine(e.Message);
                    game.EndGame();
                }
                userInput = reader.ReadLine();
                if (userInput == "")
                    userInput = null;
            }
        }
        
        private void printCommandsList()
        {
            foreach (var command in _commandsList)
            {
                Console.WriteLine(command);
            }
        }

        private void printGameInfo()
        {
            if (currentPlayer == null) return;
            if (game.Finished)
            {
                Console.WriteLine("Turn: {0}, cards: {1}, with risk: {2}", turns, game.SuccCards, game.Risks);
            }
            Console.WriteLine("Turn: {0}, Score: {1}, Finished: {2}", turns, game.Score, game.Finished);
            Console.WriteLine("{0,18}{1}", "Current player: ", currentPlayer.ToString());
            Console.WriteLine("{0,18}{1}", "Next player: ", nextPlayer().ToString());
            Console.WriteLine("{0,18}{1}", "Table: ", game.CardTable.ToString());
        }
    }

    public class Table
    {
        private IDictionary<CardColor, Stack<Card>> cardsByColor = new Dictionary<CardColor, Stack<Card>>(5);

        public Table()
        {
            for (CardColor color = CardColor.R; color <= CardColor.Y; color++)
            {
                cardsByColor.Add(createStackForColor(color));
            }
        }

        private KeyValuePair<CardColor, Stack<Card>> createStackForColor(CardColor color)
        {
            Stack<Card> cardsStack = new Stack<Card>(5);
            cardsStack.Push(new Card(color, CardRank.Unknown));
            return new KeyValuePair<CardColor, Stack<Card>>(color, cardsStack);
        }

        public bool TryPutCard(Card newCard)
        {
            Stack<Card> cards;
            if (cardsByColor.TryGetValue(newCard.Color, out cards))
            {
                Card oldCard = cards.Peek();
                if ((newCard.Rank - oldCard.Rank) == 1)
                {
                    cards.Push(newCard);
                    return true;
                }
                else
                    return false;
            }
            else
                throw new HanabiGameException("Connot find stack on table for " + newCard.ToString());
        }

        public void Clear()
        {
            foreach (var keyValue in cardsByColor)
            {
                Stack<Card> cardStack = keyValue.Value;
                while (true)
                {
                    Card card = cardStack.Pop();
                    if (card.Rank == CardRank.Unknown)
                    {
                        cardStack.Push(card);
                        break;
                    }
                }
            }
        }

        public override string ToString()
        {
            string result = "";
            foreach (var cards in cardsByColor)
            {
                Stack<Card> cardsStack = cards.Value;
                Card card = cardsStack.Peek();
                result += card.ToString() + " ";
            }
            return result.Trim();
        }
    }

    public enum CardColor
    {
        Unknown = 0,
        R = 1,
        //Red = 1,
        G = 2,
        //Green = 2,
        B = 3,
        //Blue = 3,
        W = 4,
        //White = 4
        Y = 5,
        //Yellow = 5
    }

    public enum CardRank
    {
        Unknown,
        One,
        Two,
        Three,
        Four,
        Five
    }

    public class Card
    {
        private static int id = 0;

        public int Id { get; set; }
        public CardRank Rank { get; set; }

        public CardColor Color{ get; set; }

        public Card(CardColor color, CardRank rank)
        {
            this.Rank = rank;
            this.Color = color;
            Id = ++id;
        }

        public override int GetHashCode()
        {
            return this.Id;
        }

        public override bool Equals(object obj)
        {
            Card other = obj as Card;
            return (other != null) && (other.Id == this.Id);
        }

        public override string ToString()
        {
            string colorName = "";
            int colorNum = (int)Color;
            switch (colorNum)
            {
                case 1:
                    colorName = "R";
                    break;
                case 2:
                    colorName = "G";
                    break;
                case 3:
                    colorName = "B";
                    break;
                case 4:
                    colorName = "W";
                    break;
                case 5:
                    colorName = "Y";
                    break;
                default:
                    colorName = "";
                    break;
            }
            return string.Format("{0}{1}", colorName, (int)Rank);
        }
    }
}
