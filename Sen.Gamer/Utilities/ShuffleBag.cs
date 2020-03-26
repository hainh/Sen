using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Gamer.Utilities
{
    /// <summary>
    /// Helper class for Random number generator with even distribution.
    /// </summary>
    public class ShuffleBag
    {
        private List<char> data;

        private char currentItem;
        private int currentPosition = -1;

        private int Capacity { get { return data.Capacity; } }
        public int Size { get { return data.Count; } }

        public ShuffleBag(int initCapacity)
        {
            data = new List<char>(initCapacity);
        }

        public void Add(char item, int amount)
        {
            for (int i = 0; i < amount; i++)
                data.Add(item);

            currentPosition = Size - 1;
        }

        public char Next()
        {
            if (currentPosition < 1)
            {
                currentPosition = Size - 1;
                currentItem = data[0];

                return currentItem;
            }

            var pos = TSUtilities.Randomizer.Next(currentPosition);

            currentItem = data[pos];
            data[pos] = data[currentPosition];
            data[currentPosition] = currentItem;
            currentPosition--;

            return currentItem;
        }

        public void Reset()
        {
            currentPosition = Size - 1;
        }

        /// <summary>
        /// Create a even distribution randomizer that random from <c>0</c> to <c>count - 1</c>
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static ShuffleBag CreateSequenceGenerator(int count)
        {
            var bag = new ShuffleBag(count + 1);
            for (int i = 0; i < count; ++i)
            {
                bag.data.Add((char)i);
            }
            bag.currentPosition = bag.Size - 1;
            return bag;
        }
    }
}
