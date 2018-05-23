using System;

namespace Aurender.Core.Contents
{
    public class RatingFilterRange : Tuple<RatingFilter, RatingFilter>
    {
        public RatingFilterRange(RatingFilter min, RatingFilter max) : base(min, max)
        {
            if (min < RatingFilter.RatingNoStar || max < RatingFilter.RatingNoStar) {
                throw new InvalidOperationException("RatingFilterRange items must be NoStar or higher.");
            }

            if (min > max)
            {
                throw new InvalidOperationException("RatingFilterRange maximum value must be equal or higher than minimum value.");
            }
        }

        public int Min => Item1.ToInteger();
        public int Max => Item2.ToInteger();

        public bool IsRange() => (this.Min != this.Max);

        public static RatingFilterRange Empty { get; } = new RatingFilterRange();

        public static RatingFilterRange All { get; } = new RatingFilterRange(RatingFilter.RatingNoStar, RatingFilter.RatingFiveStars);

        public static RatingFilterRange WithoutNoRating { get; } = new RatingFilterRange(RatingFilter.RatingOneStar, RatingFilter.RatingFiveStars);

        private RatingFilterRange() : base(RatingFilter.RatingNoStar, RatingFilter.RatingNoStar)
        {
        }
    }

    static class RatingFilterExtension
    {
        public static int ToInteger(this RatingFilter ratingFilter)
        {
            int value = 0;

            switch (ratingFilter)
            {
                case RatingFilter.RatingOneStar:
                    value = 10;
                    break;

                case RatingFilter.RatingTwoStars:
                    value = 20;
                    break;

                case RatingFilter.RatingThreeStars:
                    value = 30;
                    break;

                case RatingFilter.RatingFourStars:
                    value = 40;
                    break;

                case RatingFilter.RatingFiveStars:
                    value = 50;
                    break;

                default:
                    break;
            }

            return value;
        }
    }
}
