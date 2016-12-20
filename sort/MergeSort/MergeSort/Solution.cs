using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MergeSort
{
    class Solution
    {

        int count = 0;
        List<Element> rt = new List<Element>();
        public int solution(int[] A)
        {
            Element[] Arr = new Element[A.Length];

            for (int i = 0; i < A.Length; i++)
            {
                Arr[i] = new Element { point = i, radius = A[i] };
            }

            Element[] result = MergeSort(Arr, 0, Arr.Length - 1);

            return count;

        }

        private T[] MergeSort(T[] A, int first, int last)
        {
            if (first >= last)
            {
                Element[] res = { A[first] };
                return res;
            }
            int mid = first + (last - first) / 2;
            Element[] leftArr = MergeSort(A, first, mid);
            Element[] rightArr = MergeSort(A, mid + 1, last);
            Element[] result = new Element[(leftArr.Length + rightArr.Length)];
            int i = 0, j = 0, k = 0;
            while (i < leftArr.Length && j < rightArr.Length)
            {
                if (leftArr[i].radius < rightArr[j].radius)
                {
                    if (((leftArr[i].point - leftArr[i].radius) <= (rightArr[j].point + rightArr[j].radius)) ||
                                        ((leftArr[i].point + leftArr[i].radius) >= (rightArr[j].point - rightArr[j].radius)))
                    {
                        count++;
                        rt.Add(leftArr[i]);
                        rt.Add(leftArr[i]);

                    }

                    result[k++] = leftArr[i++];

                }
                else
                {
                    if (((leftArr[i].point - leftArr[i].radius) <= (rightArr[j].point + rightArr[j].radius)) ||
                    ((leftArr[i].point + leftArr[i].radius) >= (rightArr[j].point - rightArr[j].radius)))
                    {
                        count++;
                    }

                    result[k++] = rightArr[j++];
                }
            }
            if (i < leftArr.Length)
            {
                Array.Copy(leftArr, i, result, k, leftArr.Length - i);
            }
            else if (j < rightArr.Length)
            {
                Array.Copy(rightArr, j, result, k, rightArr.Length - j);
            }
            return result;
        }


        static void Main(string[] args)
        {
            int[] A = { 1, 5, 2, 1, 4, 0 };
            Solution s = new Solution();
            int result = s.solution(A);
        }
    }
}
