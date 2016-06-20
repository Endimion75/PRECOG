using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DataModels;

namespace Common
{
    public static class ListExtension
    {
        public static void BubbleSort(this IList o)
        {
            for (int i = o.Count - 1; i >= 0; i--)
            {
                for (int j = 1; j <= i; j++)
                {
                    object o1 = o[j - 1];
                    object o2 = o[j];
                    if (((IComparable)o1).CompareTo(o2) > 0)
                    {
                        o.Remove(o1);
                        o.Insert(j, o1);
                    }
                }
            }
        }

        public static double WeightedAverage<T>(this IEnumerable<T> records, Func<T, double> value, Func<T, double> weight)
        {
            double weightedValueSum = records.Sum(x => value(x) * weight(x));
            double weightSum = records.Sum(x => weight(x));

            if (weightSum != 0)
                return weightedValueSum / weightSum;
            else
                throw new DivideByZeroException("Divided by Cero!");
        }
    }

    public static class StringExtension
    {
        public static string[] SplitXP(this string expression, string delimiter, string qualifier, bool ignoreCase)
        {
            bool qualifierState = false;
            int startIndex = 0;
            var values = new System.Collections.ArrayList();

            for (int charIndex = 0; charIndex < expression.Length - 1; charIndex++)
            {
                if ((qualifier != null)
                 & (string.Compare(expression.Substring
                (charIndex, qualifier.Length), qualifier, ignoreCase) == 0))
                {
                    qualifierState = !(qualifierState);
                }
                else if (!(qualifierState) & (delimiter != null)
                      & (string.Compare(expression.Substring
                (charIndex, delimiter.Length), delimiter, ignoreCase) == 0))
                {
                    values.Add(expression.Substring
                (startIndex, charIndex - startIndex));
                    startIndex = charIndex + 1;
                }
            }

            if (startIndex < expression.Length)
                values.Add(expression.Substring
                (startIndex, expression.Length - startIndex));

            var returnValues = new string[values.Count];
            values.CopyTo(returnValues);
            return returnValues;
        }
    }

}
