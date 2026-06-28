namespace Yujanggi.Core.Match.Movement
{
    public class Horsemovement : PatternMovement
    {
        public Horsemovement()
        {
            _steps = new Step[][]
            {
                // Up
                new [] { Step.Up, Step.LeftUp },
                new [] { Step.Up, Step.RightUp },

                // Down
                new [] { Step.Down, Step.LeftDown },
                new [] { Step.Down, Step.RightDown },

                // Left
                new [] { Step.Left, Step.LeftUp },
                new [] { Step.Left, Step.LeftDown },

                // Right
                new [] { Step.Right, Step.RightUp },
                new [] { Step.Right, Step.RightDown }
            };
        }
    }
}