using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace Aurender.Core.Contents
{
    [DebuggerDisplay("DataFilter : {Keys} {Values}")]
    public class DataFilter : Dictionary<FilterTypes, Object>, ICloneable
    {
        public override bool Equals(object obj)
        {
            var right = obj as DataFilter;

            if (right != null)
            {
                if (right.Keys.Count == this.Keys.Count)
                {
                    foreach(var myKey in this.Keys)
                    {
                        bool hasSame = false;
                        foreach(var fKey in right.Keys)
                        {
                            if (fKey == myKey)
                            {
                                hasSame = true;
                                break;
                            }
                        }

                        if (!hasSame)
                            return false;
                    }

                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            FilterTypes key = 0;
            foreach(var k in this.Keys)
            {
                key |= key;
            }


            return ((int) key).GetHashCode();
        }
        /// <summary>
        /// If filter is not set, returns -1;
        /// </summary>
        public Int32 FolderFilter
        {
            get => GetIntFor(FilterTypes.FolderIndex);
            set => this[FilterTypes.FolderIndex] = value;
        }


        /// <summary>
        /// When set this value, the value is day.
        /// If filter is not set, returns -1;
        /// </summary>
        public Int32 AddedDateFilter
        {
            get => GetIntFor(FilterTypes.AddedDate);
            set => this[FilterTypes.AddedDate] = value;
        }


        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("Filter : ");

            foreach(var key in this.Keys)
            {
                sb.AppendFormat(" [{0}],", key);
            }

            if (this.Keys.Count > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        /// <summary>
        /// If filter is not set, returns -1;
        /// </summary>
        public RatingFilterRange RatingFilter
        {
            get => this.ContainsKey(FilterTypes.Rating) ? (RatingFilterRange)this[FilterTypes.Rating] : RatingFilterRange.Empty;
            set => this[FilterTypes.Rating] = value;
        }
        public String Keyword
        {
            get => this[FilterTypes.Keyword].ToString();
            set => this[FilterTypes.Keyword] = value;
        }

        /// <summary>
        /// If filter is not set, returns AudioProperty;
        /// </summary>
        public AudioPropertyFilter AudioPropertyFilter
        {
            get => (AudioPropertyFilter)GetIntFor(FilterTypes.AudioProperty);
            set => this[FilterTypes.AddedDate] = value;
        }

        /// <summary>
        /// Tokenized keyword for query generation.
        /// </summary>
        /// <value>The keywords.</value>
        public String[] Keywords
        {
            get => Keyword.Split(' ');
        }

        private Int32 GetIntFor(FilterTypes filter)
        {
            if (ContainsKey(filter))
            {
                var obj = this[filter];
                if (obj != null)
                {
                    return Convert.ToInt32(obj);
                }
            }

            if (filter == FilterTypes.AudioProperty)
            {
                return (Int32)AudioPropertyFilter.F_NONE;
            }

            return -1;
        }

        public object Clone()
        {
            var filter = new DataFilter();

            foreach (var item in this)
            {
                filter.Add(item.Key, item.Value);
            }

            return filter;
        }
    }


    public static class FilterFactory
    {
        static void AddDateFilter(DataFilter filter)
        {
            filter.Add(FilterTypes.AddedDate, 100);
        }
        static void AddFolderFilter(DataFilter filter)
        {
            filter.Add(FilterTypes.FolderIndex, 1);
        }
        static void AddAudioFilter(DataFilter filter)
        {
            filter.Add(FilterTypes.AudioProperty, AudioPropertyFilter.F_24Bit);
        }
        static void AddRatingFilter(DataFilter filter)
        {
            RatingFilterRange rFilter = new RatingFilterRange(RatingFilter.RatingOneStar, RatingFilter.RatingFiveStars);
            filter.Add(FilterTypes.Rating, rFilter);
        }
        static void AddKeywordFilter(DataFilter filter)
        {
            filter.Add(FilterTypes.Keyword, "hello");
        }

        public static Tuple<HashSet<DataFilter>, List<Action<DataFilter>>> Permutations( Tuple<HashSet<DataFilter>, List<Action<DataFilter>>> list)
        {
            if (list.Item2.Count == 1)
            {
                DataFilter filter1 = new DataFilter();

                list.Item2[0](filter1);

                if (!list.Item1.Contains(filter1))
                {
                      list.Item1.Add(filter1);

                }

                return new Tuple<HashSet<DataFilter>, List<Action<DataFilter>>> (list.Item1, new List<Action<DataFilter>>());
            }

            List<Action<DataFilter>> pms = new List<Action<DataFilter>>(); 

                DataFilter filter = new DataFilter();
            foreach (Action<DataFilter> element in list.Item2)
            { // For each element in that list

                var remainingList = new List<Action<DataFilter>>(list.Item2);
                remainingList.Remove(element); // Get a list containing everything except of chosen element
                var param =  new Tuple<HashSet<DataFilter>, List<Action<DataFilter>>>(list.Item1, remainingList);

                var perms = Permutations(param);

                element(filter);


                foreach (Action<DataFilter> permutation in remainingList)
                { // Get all possible sub-permutations

                //    permutation(filter);

                   // list.Item1.Add(filter);
                } 
            }
            if (!list.Item1.Contains(filter))
                list.Item1.Add(filter);
            
            return new Tuple<HashSet<DataFilter>, List<Action<DataFilter>>>(list.Item1, pms);

        }

        public static  HashSet<DataFilter> AllPossibleFilters()
        {


            List<Action<DataFilter>> list = new List<Action<DataFilter>>()
            {
                AddDateFilter,
                AddFolderFilter,
                AddAudioFilter,
                AddRatingFilter,
                AddKeywordFilter,
            };

            HashSet<DataFilter> filters = new HashSet<DataFilter>();

            var param = new Tuple<HashSet<DataFilter>, List<Action<DataFilter>>>(filters, list);

            foreach(var a in list)
            {
                var f = new DataFilter();
                a(f);

          //      filters.Add(f);
            }

            var result = Permutations(param);


            return result.Item1;
        }
    }
}
