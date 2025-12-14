using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace G2LR0
{
    internal struct Item
    {
        public bool isEpsilon;
        public int index = -1;
        public int viablePrefixIndex;
        public string left = "";
        public List<string> right = new();
        public Item(string left, List<string> right, int viablePrefixIndex, int index)
        {
            this.left = left;
            this.right = right;
            this.viablePrefixIndex = viablePrefixIndex;
            this.index = index;
            isEpsilon = right.Count == 1 && right[0] == Grammar.Epsilon;
        }

        /// <summary>
        /// 生成该项目的闭包
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public List<Item> GenerateClosure(Dictionary<(int index, int viablePrefixIndex), Item> dict,
                                  Dictionary<string, HashSet<int>> itemIndex, Grammar grammar)
        {
            List<Item> closure = new();
            Queue<Item> stack = new();
            stack.Enqueue(this);
            while (stack.TryDequeue(out var result))
            {
                closure.Add(result);
                if (result.isEpsilon) continue;
                string? next = result.right.ElementAtOrDefault(result.viablePrefixIndex);
                if (next == null || grammar.IsTerminal(next)) continue;
                foreach (var prodIndex in itemIndex[next])
                {
                    var newItem = dict[(prodIndex, 0)];
                    if (!closure.Contains(newItem) && !stack.Contains(newItem))
                    {
                        stack.Enqueue(newItem);
                    }
                }
            }
            return closure;
        }
        public bool TryGoto(Dictionary<(int, int), Item> itemDict, out Item item)
        {
            if(itemDict.TryGetValue((this.index, this.viablePrefixIndex + 1),out item))
                return true;
            return false;
        }
        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append($"[{index},{viablePrefixIndex}]: {left} -> ");
            if (isEpsilon)
            {
                stringBuilder.Append("·");
                return stringBuilder.ToString();
            }
            for (int i = 0; i <= right.Count; i++)
            {

                if (i == viablePrefixIndex)
                {
                    stringBuilder.Append("· ");
                }
                if (i < right.Count)
                    stringBuilder.Append($"{right[i]} ");
            }
            return stringBuilder.ToString();
        }

    }
}
