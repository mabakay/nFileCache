/*
Copyright 2015 mabakay, based on FileCache (http://fc.codeplex.com)

This file is part of nFileCache (https://github.com/mabakay/nFileCache).

nFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

using System.Collections.Generic;

namespace System.Runtime.Caching
{
    internal class CacheItemExpirationDateComparer : IComparer<Tuple<DateTime, string>>
    {
        public int Compare(Tuple<DateTime, string> x, Tuple<DateTime, string> y)
        {
            int result = x.Item1.CompareTo(y.Item1);

            return result == 0 ? 1 : result;
        }
    }
}