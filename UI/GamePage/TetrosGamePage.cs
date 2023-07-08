using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Tetros;


public partial class TetrosGamePage : Panel
{
    public Panel BoardPanel {get; set;}
    public Panel[] Blocks {get; set;} = new Panel[200];
    public Panel[] CurrentBlocks {get; set;} = new Panel[4];
    public Panel[] GhostBlocks {get; set;} = new Panel[4];

    // Game Variables
    const int BOARD_WIDTH = 10;
    const int QUEUE_LENGTH = 5;
    public BlockType HeldPiece {get; set;} = BlockType.Empty;
    public int Level {get; set;} = 1;
    public int LinesNeeded {get; set;} = 10;
    private List<BlockType> GrabBag {get; set;} = new List<BlockType>();
    private List<BlockType> Queue {get; set;} = new List<BlockType>();
    public List<BlockType> Board {get; set;} = new List<BlockType>();
    public BlockType CurrentPiece {get; set;} = new BlockType();
    public long Score {get; set;} = 0;
    public long HighScore {get; set;} = 0;
    public int CurrentPieceX {get; set;}
    public int CurrentPieceY {get; set;}
    public int CurrentPieceRotation {get; set;} = 0;
    public bool FastDrop {get; set;} = false;
    public bool JustHeld {get; set;} = false;
    public int Combo {get; set;} = -1;
    public bool Playing {get; set;} = false;
    private RealTimeSince LastUpdate = 0f;
    private RealTimeSince LeftTimer = 0f;
    private RealTimeSince RightTimer = 0f;

    private TetrosMenu Menu;

    private Entity SoundEntity => null; 

    protected override void OnAfterTreeRender(bool firstTime)
    {
        base.OnAfterTreeRender(firstTime);

        if(firstTime)
        {
            Board = new List<BlockType>();
            for(int i=0; i<200; i++)
            {
                Board.Add(BlockType.Empty);
            }

            for(int i=0; i<200; i++)
            {
                var block = BoardPanel.Add.Panel("block");
                Blocks[i] = block;
            }

            for(int i=0; i<4; i++)
            {
                CurrentBlocks[i] = BoardPanel.Add.Panel("block current");
            }

            for(int i=0; i<4; i++)
            {
                GhostBlocks[i] = BoardPanel.Add.Panel("block ghost");
            }

            // Connect all the actions
            if(Ancestors.FirstOrDefault(x => x is TetrosMenu) is TetrosMenu menu)
            {
                Menu = menu;
                Menu.UpdateBoard += UpdateBoard;
                Menu.UpdatePlayer += UpdatePlayer;
                Menu.UpdateHeldPiece += UpdateHeldPiece;
                Menu.UpdateNextPieces += UpdateNextPieces;
                Menu.UpdateScore += UpdateScore;
                Menu.StartGame += StartGame;
                Menu.EndGame += EndGame;
            }

            StartGame();
        }
    }


    public void StartGame()
    {
        Log.Info("STARTING TETROS GAME!!!");

        Queue.Clear();
        for(int i=0; i<QUEUE_LENGTH; i++)
        {
            var block = GetRandomBlock();
            Queue.Add(block);
            Log.Info($"Queue[{i}] = {Queue[i]}");
        }

        Score = 0;
        Combo = -1;
        Level = 1;
        LinesNeeded = 10;
        Playing = true;
    }

    public void EndGame()
    {
        SaveHighScore();
        
        Menu?.OnExit(Score);

        CurrentPiece = BlockType.Empty;
        Board.Clear();
        for(int i=0; i<200; i++)
        {
            Board.Add(BlockType.Empty);
        }
        GrabBag.Clear();
        Queue.Clear();
        HeldPiece = BlockType.Empty;
        Score = 0;
        Playing = false;

        Menu?.Navigate("/");
    }

    public void LoadHighScore()
    {
        HighScore = Cookie.Get<long>("home.arcade.tetros.hiscore", 0);
        UpdateHighScore(Score);
    }

    public void SaveHighScore()
    {
        if(Score > Cookie.Get<long>("home.arcade.tetros.hiscore", 0))
        {
            Cookie.Set("home.arcade.tetros.hiscore", Score);
        }
    }

    [GameEvent.Client.Frame]
    public void OnFrame()
    {
        if(!Playing) return;

        var interval = GetWaitTime();
        if(FastDrop) interval = MathF.Min(0.04f, interval / 4f);
        if(Playing && LastUpdate > interval)
        {
            if(CurrentPiece == BlockType.Empty)
            {
                GetNewBlock();
            }
            else
            {
                CurrentPieceY += 1;
                if(CheckPieceCollision(CurrentPiece, CurrentPieceRotation, new Vector2(CurrentPieceX, CurrentPieceY)))
                {
                    CurrentPieceY -= 1;
                    PlacePiece();
                }
                else if(FastDrop)
                {
                    if(SoundEntity == null)
                        Sound.FromScreen("tetros_move").SetPitch(1.5f);
                    else
                        Sound.FromEntity("tetros_move", SoundEntity).SetPitch(1.5f);

                    Score += 1;
                }
                RequestUpdatePlayer();
            }
            LastUpdate = 0f;
            if(CheckPieceCollision(CurrentPiece, CurrentPieceRotation, new Vector2(CurrentPieceX, CurrentPieceY + 1)))
            {
                LastUpdate = -GetWaitTime()/4;
            }
        }
    }

    [GameEvent.Client.BuildInput]
    public void BuildInput()
    {
        if(!Playing) return;

        if(Input.Pressed("TetrosHardDrop"))
        {
            HardDrop();
        }
        else if(Input.Pressed("TetrosHold"))
        {
            Hold();
        }
        else
        {
            if(Input.Pressed("TetrosMoveLeft"))
            {
                LeftTimer = 0f;
                Move(-1);
            }
            else if(Input.Down("TetrosMoveLeft") && LeftTimer > 0.2f)
            {
                LeftTimer = 0.1f;
                Move(-1);
            }

            if(Input.Pressed("TetrosMoveRight"))
            {
                RightTimer = 0f;
                Move(1);
            }
            else if(Input.Down("TetrosMoveRight") && RightTimer > 0.2f)
            {
                RightTimer = 0.1f;
                Move(1);
            }

            if(Input.Pressed("TetrosRotateRight"))
            {
                Rotate();
            }
            if(Input.Pressed("TetrosRotateLeft"))
            {
                Rotate(-1);
            }
            FastDrop = Input.Down("TetrosSoftDrop");
        }
    }

    #region UI UPDATING

        private void RequestUpdateBoard()
        {
            int[] board = new int[200];
            for(int i=0; i<200; i++)
            {
                board[i] = (int)Board[i];
            }
            Menu?.ServerUpdateBoard.Invoke(BoardToString(board));
            
            UpdateBoard(board);
        }

        public void UpdateBoard(int[] board)
        {
            for(int i=0; i<200; i++)
            {
                int val = board[i];
                Blocks[i].SetClass("t-1 t-2 t-3 t-4 t-5 t-6 t-7", false);
                Blocks[i].SetClass("active t-" + val.ToString(), val != 0);
            }
        }

        private void RequestUpdateScore()
        {
            //TODO: Action to request update score
            //ArcadeMachineTetros.RequestScore(Machine.NetworkIdent, Score);
            UpdateScore(Score);
        }

        public void UpdateScore(long score)
        {
            if(Style.Opacity == 0f) return;
            
            Score = score;
        }

        private void RequestUpdatePlayer()
        {
            Menu?.ServerUpdatePlayer.Invoke(CurrentPiece, (int)CurrentPieceX, (int)CurrentPieceY, CurrentPieceRotation);
            UpdatePlayer(CurrentPiece, new Vector2(CurrentPieceX, CurrentPieceY), CurrentPieceRotation);
        }

        public void UpdatePlayer(BlockType blockType, Vector2 pos, int rotation)
        {
            if(Style.Opacity == 0f) return;

            if(blockType == BlockType.Empty)
            {
                for(int i=0; i<4; i++)
                {
                    CurrentBlocks[i].Style.Left = -200;
                    CurrentBlocks[i].Style.Top = -200;
                }
                return;
            }

            for(int i=0; i<4; i++)
            {
                CurrentBlocks[i].SetClass("t-1 t-2 t-3 t-4 t-5 t-6 t-7", false);
                CurrentBlocks[i].SetClass("t-" + ((int)blockType).ToString(), blockType != BlockType.Empty);
            }

            SetPositionFromPiece(CurrentBlocks, blockType, pos, rotation);

            // Calculate ghost position
            int ghostY = (int)pos.y;
            while(!CheckPieceCollision(blockType, rotation, new Vector2(pos.x, ghostY)))
            {
                ghostY++;
            }
            ghostY--;
            SetPositionFromPiece(GhostBlocks, blockType, new Vector2(pos.x, ghostY), rotation);
        }

        private void RequestHeldPiece()
        {
            Menu?.ServerRequestHeldPiece(HeldPiece);
            UpdateHeldPiece(HeldPiece);
        }

        public void UpdateHeldPiece(BlockType blockType)
        {
            if(Style.Opacity == 0f) return;

            HeldPiece = blockType;
        }

        private void RequestNextPieces()
        {
            int[] nextPieces = new int[Queue.Count()];
            for(int i=0; i<Queue.Count(); i++)
            {
                nextPieces[i] = (int)Queue[i];
            }
            Menu?.ServerRequestNextPieces(BoardToString(nextPieces));
        }

        public void UpdateNextPieces(BlockType[] blockTypes)
        {
            if(Style.Opacity == 0f) return;

            Queue = blockTypes.ToList();
        }

        public void UpdateHighScore(long score)
        {
            if(Style.Opacity == 0f) return;
            HighScore = score;
        }

        // public void ShowAll()
        // {
        //     var panelList = new List<Panel>();
        //     panelList.AddRange(CurrentBlocks);
        //     panelList.AddRange(GhostBlocks);
        //     for(int i=0; i<5; i++)
        //     {
        //         panelList.AddRange(NextBlocks[i]);
        //     }
        //     panelList.AddRange(HoldBlocks);
        //     foreach(var block in panelList)
        //     {
        //         block.RemoveClass("hide");
        //     }
        // }

        // public void HideAll()
        // {
        //     var panelList = new List<Panel>();
        //     panelList.AddRange(CurrentBlocks);
        //     panelList.AddRange(GhostBlocks);
        //     for(int i=0; i<5; i++)
        //     {
        //         panelList.AddRange(NextBlocks[i]);
        //     }
        //     panelList.AddRange(HoldBlocks);
        //     foreach(var block in panelList)
        //     {
        //         block.AddClass("hide");
        //     }
        // }

    #endregion

    #region GRAB BAG / QUEUE

        public void GetNewBlock()
        {
            CurrentPiece = GetPieceFromQueue();
            CurrentPieceX = 5;
            CurrentPieceY = -2;
            CurrentPieceRotation = 0;
            RequestUpdatePlayer();
        }

        public BlockType GetRandomBlock()
        {
            if(GrabBag.Count < QUEUE_LENGTH)
            {
                GrabBag = new List<BlockType> { BlockType.I, BlockType.O, BlockType.T, BlockType.S, BlockType.Z, BlockType.J, BlockType.L };
                // Shuffle the grab bag
                GrabBag = GrabBag.OrderBy(x => Guid.NewGuid()).ToList();
            }

            var block = GrabBag[0];
            GrabBag.RemoveAt(0);

            RequestNextPieces();
            return block;
        }

        public BlockType GetPieceFromQueue()
        {
            var block = Queue[0];
            Queue.RemoveAt(0);
            var newBlock = GetRandomBlock();
            Queue.Add(newBlock);
            return block;
        }

    #endregion


    #region COLLISION CHECK

        public bool CheckPieceCollision(BlockType block, int rot, Vector2 pos)
        {
            var piece = TetrosShapes.GetShape(block, rot);
            var grid = piece.GetGrid();
            for(int i=0; i<16; i++)
            {
                if(grid[i] == 1)
                {
                    int x = (int)pos.x + (i % 4) - 1;
                    int y = (int)pos.y + (i / 4) - 1;
                    if(x < 0 || x >= BOARD_WIDTH || y >= 20)
                    {
                        return true;
                    }

                    int ipos = x + (y * BOARD_WIDTH);
                    if(ipos >= 0 && ipos < Board.Count && Board[ipos] != BlockType.Empty)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    #endregion

    private void PlacePiece()
    {
        var piece = TetrosShapes.GetShape(CurrentPiece, CurrentPieceRotation);
        var grid = piece.GetGrid();
        for(int i=0; i<16; i++)
        {
            if(grid[i] == 1)
            {
                var x = CurrentPieceX + (i % 4) - 1;
                var y = CurrentPieceY + (i / 4) - 1;
                int pos = x + (y * BOARD_WIDTH);
                if(pos < 0)
                {
                    // TODO: Action for game over
                    EndGame();
                    return;
                }
                if(pos < Board.Count)
                {
                    Board[pos] = CurrentPiece;
                }
            }
        }
        JustHeld = false;
        if(SoundEntity == null)
            Sound.FromScreen("tetros_place");
        else
            Sound.FromEntity("tetros_place", SoundEntity);
        CurrentPiece = BlockType.Empty;
        GetNewBlock();
        
        RequestUpdateBoard();
        CheckLine();
    }

    #region LINE CHECK

        private void CheckLine()
        {
            int lines = 0;
            for(int y=0; y<20; y++)
            {
                bool line = true;
                for(int x=0; x<BOARD_WIDTH; x++)
                {
                    int pos = x + (y * BOARD_WIDTH);
                    if(Board[pos] == BlockType.Empty)
                    {
                        line = false;
                        break;
                    }
                }
                if(line)
                {
                    for(int x=0; x<BOARD_WIDTH; x++)
                    {
                        int pos = x + (y * BOARD_WIDTH);
                        Board[pos] = BlockType.Empty;
                    }
                    for(int i=y; i>0; i--)
                    {
                        for(int x=0; x<BOARD_WIDTH; x++)
                        {
                            int pos = x + (i * BOARD_WIDTH);
                            int prevPos = x + ((i-1) * BOARD_WIDTH);
                            Board[pos] = Board[prevPos];
                        }
                    }
                    lines++;
                }
            }
            if(lines > 0)
            {
                Sound sound;
                if(SoundEntity == null)
                    sound = Sound.FromScreen("tetros_line");
                else
                    sound = Sound.FromEntity("tetros_line", SoundEntity);
                sound.SetPitch(1f + (MathF.Max(0, Combo) * (1.0f/12.0f)));

                Combo += 1;
                switch(lines)
                {
                    case 1:
                        Score += 100 * Level;
                        break;
                    case 2:
                        Score += 300 * Level;
                        break;
                    case 3:
                        Score += 500 * Level;
                        break;
                    case 4:
                        if(SoundEntity == null)
                            Sound.FromScreen("tetros_tetros");
                        else
                            Sound.FromEntity("tetros_tetros", SoundEntity);
                        Score += 800 * Level;
                        break;
                }
                if(Combo > 0)
                {
                    Score += 50 * (Combo * Level);
                }
                LinesNeeded -= lines;
                if(LinesNeeded <= 0 && Level < 20)
                {
                    Level += 1;
                    if(Level >= 10 && Level <= 15) LinesNeeded += 100;
                    else if(Level > 15) LinesNeeded += 100 + ((Level - 15) * 10);
                    else LinesNeeded += Level * 10;
                }
                RequestUpdateBoard();
                RequestUpdateScore();
            }
            else
            {
                Combo = -1;
            }
        }

    #endregion

    #region CONTROLS

        public void Move(int dir)
        {
            CurrentPieceX += MathF.Sign(dir);
            if(CheckPieceCollision(CurrentPiece, CurrentPieceRotation, new Vector2(CurrentPieceX, CurrentPieceY)))
            {
                CurrentPieceX -= MathF.Sign(dir);
            }
            if(SoundEntity == null)
                Sound.FromScreen("tetros_move");
            else
                Sound.FromEntity("tetros_move", SoundEntity);
            RequestUpdatePlayer();
        }

        public void Rotate(int dir = 1)
        {
            int prevRot = CurrentPieceRotation;
            CurrentPieceRotation += dir;
            while(CurrentPieceRotation < 0) CurrentPieceRotation += 4;
            CurrentPieceRotation = CurrentPieceRotation % 4;
            if(CheckPieceCollision(CurrentPiece, CurrentPieceRotation, new Vector2(CurrentPieceX, CurrentPieceY)))
            {
                CurrentPieceRotation = prevRot;
            }
            LastUpdate /= 2;
            if(SoundEntity == null)
                Sound.FromScreen("tetros_rotate");
            else
                Sound.FromEntity("tetros_rotate", SoundEntity);
            RequestUpdatePlayer();
        }

        public void HardDrop()
        {
            while(!CheckPieceCollision(CurrentPiece, CurrentPieceRotation, new Vector2(CurrentPieceX, CurrentPieceY)))
            {
                CurrentPieceY += 1;
                Score += 2;
            }
            RequestUpdateScore();
            Score -= 2;
            CurrentPieceY -= 1;
            PlacePiece();
            LastUpdate = GetWaitTime()/4f * 3f;
        }

        public void Hold()
        {
            if(JustHeld) return;

            if(HeldPiece == BlockType.Empty)
            {
                HeldPiece = CurrentPiece;
                CurrentPiece = BlockType.Empty;
                GetNewBlock();
            }
            else
            {
                var temp = HeldPiece;
                HeldPiece = CurrentPiece;
                CurrentPiece = temp;
            }
            CurrentPieceX = 5;
            CurrentPieceY = -2;
            CurrentPieceRotation = 0;
            JustHeld = true;
            if(SoundEntity == null)
                Sound.FromScreen("tetros_hold");
            else
                Sound.FromEntity("tetros_hold", SoundEntity);

            RequestHeldPiece();
            RequestUpdatePlayer();
        }

    #endregion

    #region HELPER FUNCTIONS

        public void SetPositionFromPiece(Panel[] panel, BlockType blockType, Vector2 pos, int rotation)
        {
            TetrosShape shape = TetrosShapes.GetShape(blockType, rotation);
            int index = 0;
            for(int i=0; i<16; i++)
            {
                int x = i % 4;
                int y = i / 4;
                int x2 = x - 1;
                int y2 = y - 1;
                int x3 = (int)pos.x + x2;
                int y3 = (int)pos.y + y2;
                if(shape.Blocks.Contains(i))
                {
                    panel[index].Style.Left = x3 * 10f;
                    panel[index].Style.Top = y3 * 10f;
                    index++;
                }
            }
        }

        public float GetWaitTime()
        {
            switch(Level)
            {
                case 0: return 36f/60f;
                case 1: return 32f/60f;
                case 2: return 29f/60f;
                case 3: return 25f/60f;
                case 4: return 22f/60f;
                case 5: return 18f/60f;
                case 6: return 15f/60f;
                case 7: return 11f/60f;
                case 8: return 7f/60f;
                case 9: return 5f/60f;
                case 10:
                case 11:
                case 12:
                    return 4f/60f;
                case 13:
                case 14:
                case 15:
                    return 3f/60f;
                case 16:
                case 17:
                case 18:
                    return 2f/60f;
                case 19: return 1f/60f;
                case 20: return 1f/60f;
                default: return 0.01f;
            }
        }

        public static string BoardToString(int[] board)
        {
            string str = "";
            for(int i=0; i<board.Length; i++)
            {
                str += board[i].ToString();
            }
            return str;
        } 

        public static int[] StringToBoard(string str)
        {
            int[] board = new int[str.Length];
            for(int i=0; i<board.Length; i++)
            {
                board[i] = int.Parse(str[i].ToString());
            }
            return board;
        }

	#endregion

	protected override int BuildHash()
	{
        return HashCode.Combine(Score, Level, HeldPiece, Queue);
	}

}