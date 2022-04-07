using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sen.Utilities
{
	public class WeightedRandomizer<T>
	{
		private int seed;
		private List<WeightedChance<T>> elementList;
		private int totalWeight;

		public List<WeightedChance<T>> ElementList => elementList;


		public WeightedRandomizer(int maxValue = 10, int _seed = 0)
		{
			this.elementList = new List<WeightedChance<T>>(maxValue);
			totalWeight = 0;
			//if (_seed != 0)
			//{
			//	Random
			//	Random.InitState(_seed);
			//}
			//seed = _seed;
		}

		public void SetSeed(int seed)
		{
		}

		public void ClearElementList() { elementList.Clear(); }

		public void AddOrUpdateValue(T value, int weight)
		{
			WeightedChance<T> element = TryGetValue(value);
			if (element == null)
			{
				elementList.Add(new WeightedChance<T>(value, weight));

			}
			else
			{
				element.Weight = weight;
			}

			AdjustWeight();
		}

		public int GetWeight(T value)
		{
			WeightedChance<T> element = TryGetValue(value);
			if (element == null)
			{
				return 0;
			}
			else
			{
				return element.Weight;
			}
		}

		private void AdjustWeight()
		{
			totalWeight = 0;
			for (int i = 0; i < elementList.Count; i++)
			{
				totalWeight += elementList[i].Weight;
				elementList[i].AdjustedWeight = totalWeight;
			}
		}

		public T GetRandom()
		{
			int randomNumber = Sen.Utilities.ThreadSafeRandom.Current.Next(0, totalWeight);
			for (int i = 0; i < elementList.Count; i++)
			{
				if (randomNumber < elementList[i].AdjustedWeight)
				{
					return elementList[i].Value;
				}
			}

			return default(T);
		}

		private WeightedChance<T> TryGetValue(T value)
		{
			for (int i = 0; i < elementList.Count; i++)
			{
				if (Equals(elementList[i].Value, value))
				{
					return elementList[i];
				}
			}

			return null;
		}
	}
}
