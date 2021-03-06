﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PoESkillTree.TreeGenerator.Genetic
{
    /// <summary>
    ///  A generic class storing entries that can be randomly sampled from. The
    ///  probability of a particular entry being picked as a sample is directly
    ///  proportional to its weight.
    /// </summary>
    /// <typeparam name="T">The type of the stored objects.</typeparam>
    public class WeightedSampler<T>
    {
        /// The basic idea is to generate the (discrete) cumulative distribution
        /// function (CDF) and then randomly sample from its value range (which
        /// equals the sum of all weights).
        /// 
        /// Note that removing elements is not implemented!
        
        private readonly SortedDictionary<double, T> _entries;
        private double _totalWeight;

        private readonly Random _random;

        /// <summary>
        /// The entries SortedDictionary as Array. Enables index access and therefore binary search.
        /// </summary>
        private KeyValuePair<double, T>[]? _indexedEntries;
        /// <summary>
        /// If RandomSample uses indexed entries. Gets disabled if AddEntry is called after RandomSample.
        /// </summary>
        private bool _indexingEnabled = true;

        /// <summary>
        ///  Indicates whether any entries are present to sample from.
        /// </summary>
        public bool CanSample => _totalWeight > 0;

        /// <summary>
        ///  A new instance of the WeightedSampler class.
        /// </summary>
        /// <param name="random">A random number generator. If nothing is passed, a
        /// new one is created.</param>
        public WeightedSampler(Random? random = null)
        {
            _entries = new SortedDictionary<double, T>();
            _totalWeight = 0;

            _random = random ?? new Random();
        }

        /// <summary>
        ///  Adds a new entry with a specified weight to the sampler.
        /// </summary>
        /// <param name="entry">The object that should be randomly selected.</param>
        /// <param name="weight">A value proportional to the object's chance to be
        /// selected.</param>
        public void AddEntry(T entry, double weight)
        {
            if (double.IsInfinity(weight))
                throw new ArgumentException("Infinite weights are not allowed!", nameof(weight));
            if (weight < 0)
                throw new ArgumentException("Negative weights are not allowed!", nameof(weight));

            // No need to sample 0 weight individuals;
            if (weight <= 0) return;

            _totalWeight += weight;

            // This is where the CDF comes from.
            _entries.Add(_totalWeight, entry);

            if (_indexedEntries != null && _indexingEnabled)
            {
                // If AddEntry() is called after RandomSample() is called, indexing is not
                // better since it needs to be done more than once.
                _indexingEnabled = false;
                _indexedEntries = null;
            }
        }

        /// <summary>
        ///  Draw a random element from the stored entries. Each element's chance to
        ///  be drawn here is proportional to the weight it was inserted with.
        /// </summary>
        /// <returns>The randomly drawn element.</returns>
        public T RandomSample()
        {
            if (_indexedEntries == null && _indexingEnabled)
            {
                _indexedEntries = _entries.ToArray();
            }

            // Randomly sample the CDF.
            double r = _random.NextDouble() * _totalWeight;

            // We want to find the first entry with key >= r.
            if (_indexedEntries != null)
            {
                // If the entries are accessible via index, binary search is fastest. O(log n)
                // At least for entry counts where it matters.
                int i, imin = 0, imax = _indexedEntries.Length - 1;
                for (i = (imin + imax) / 2; i < imax; i = (imin + imax) / 2)
                {
                    var key = _indexedEntries[i].Key;
                    if (key < r)
                        imin = i + 1;
                    else
                        imax = i - 1;
                }
                // Above search either returns the correct index or is the correct index - 1.
                if (_indexedEntries[i].Key < r) i++;
                return _indexedEntries[i].Value;
            }
            else
            {
                // If AddEntry() is called between RandomSample()-calls, indexing is slower than
                // a simple linear search. (O(n))
                var entry = _entries.First(kvp => kvp.Key >= r);

                return entry.Value;
            }
        }
    }
}
