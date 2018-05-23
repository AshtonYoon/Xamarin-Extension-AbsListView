using System;

namespace Aurender.Core
{
    [Flags]
    public enum RatingFilter
    {
        RatingNoFilter   = 0,
        RatingNoStar     = 1,
        RatingOneStar    = 2,
        RatingTwoStars   = 4,
        RatingThreeStars = 8,
        RatingFourStars  = 16,
        RatingFiveStars  = 32,
    }
}
