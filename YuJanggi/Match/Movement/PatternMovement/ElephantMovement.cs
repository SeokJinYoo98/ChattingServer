namespace Yujanggi.Core.Match.Movement
{
    public class ElephantMovement : PatternMovement
    {
        public ElephantMovement()
        {
            _steps = new Step[][]
            {
                // Up
                new [] { Step.Up, Step.LeftUp, Step.LeftUp  },
                new [] { Step.Up, Step.RightUp, Step.RightUp },

                // Down
                new [] { Step.Down, Step.LeftDown, Step.LeftDown },
                new [] { Step.Down, Step.RightDown, Step.RightDown },

                // Left
                new [] { Step.Left, Step.LeftUp, Step.LeftUp },
                new [] { Step.Left, Step.LeftDown, Step.LeftDown },

                // Right
                new [] { Step.Right, Step.RightUp, Step.RightUp },
                new [] { Step.Right, Step.RightDown, Step.RightDown }
            };
        }
    }
}