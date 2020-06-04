using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Sen.Utilities
{
    /// <summary>
    /// Optimize Read only dictionary for less than or equal 20 keys dictionaries.
    /// Use with simple <see cref="TKey"/> types to benefit from fast comparation
    /// and hash code computation.
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    public class LessKeyReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue>
            _fallbackDictionary = new Dictionary<TKey, TValue>();
        private readonly bool _useFallbackDictionary;
        private readonly bool _hasValue;
        private readonly int _count;
        private readonly KeyValuePair<TKey, TValue> _pair0;
        private readonly KeyValuePair<TKey, TValue> _pair1;
        private readonly KeyValuePair<TKey, TValue> _pair2;
        private readonly KeyValuePair<TKey, TValue> _pair3;
        private readonly KeyValuePair<TKey, TValue> _pair4;
        private readonly KeyValuePair<TKey, TValue> _pair5;
        private readonly KeyValuePair<TKey, TValue> _pair6;
        private readonly KeyValuePair<TKey, TValue> _pair7;
        private readonly KeyValuePair<TKey, TValue> _pair8;
        private readonly KeyValuePair<TKey, TValue> _pair9;
        private readonly KeyValuePair<TKey, TValue> _pair10;
        private readonly KeyValuePair<TKey, TValue> _pair11;
        private readonly KeyValuePair<TKey, TValue> _pair12;
        private readonly KeyValuePair<TKey, TValue> _pair13;
        private readonly KeyValuePair<TKey, TValue> _pair14;

        public LessKeyReadOnlyDictionary(Dictionary<TKey, TValue> messageHandlers)
        {
            var count = _count = messageHandlers.Count;
            _hasValue = count > 0;
            _useFallbackDictionary = count > 15;
            _fallbackDictionary = messageHandlers;
            if (_useFallbackDictionary || !_hasValue)
            {
                return;
            }

            _pair0 = messageHandlers.ElementAt(0);
            if (count > 1)
            {
                _pair1 = messageHandlers.ElementAt(1);
            }
            if (count > 2)
            {
                _pair2 = messageHandlers.ElementAt(2);
            }
            if (count > 3)
            {
                _pair3 = messageHandlers.ElementAt(3);
            }
            if (count > 4)
            {
                _pair4 = messageHandlers.ElementAt(4);
            }
            if (count > 5)
            {
                _pair5 = messageHandlers.ElementAt(5);
            }
            if (count > 6)
            {
                _pair6 = messageHandlers.ElementAt(6);
            }
            if (count > 7)
            {
                _pair7 = messageHandlers.ElementAt(7);
            }
            if (count > 8)
            {
                _pair8 = messageHandlers.ElementAt(8);
            }
            if (count > 9)
            {
                _pair9 = messageHandlers.ElementAt(9);
            }
            if (count > 10)
            {
                _pair10 = messageHandlers.ElementAt(10);
            }
            if (count > 11)
            {
                _pair11 = messageHandlers.ElementAt(11);
            }
            if (count > 12)
            {
                _pair12 = messageHandlers.ElementAt(12);
            }
            if (count > 13)
            {
                _pair13 = messageHandlers.ElementAt(13);
            }
            if (count > 14)
            {
                _pair14 = messageHandlers.ElementAt(14);
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }
                while (_hasValue)
                {
                    if (_useFallbackDictionary)
                    {
                        return _fallbackDictionary[key];
                    }
                    var count = _count;
                    if (_pair0.Key.Equals(key))
                    {
                        return _pair0.Value;
                    }
                    if (count == 1) break;
                    if (_pair1.Key.Equals(key))
                    {
                        return _pair1.Value;
                    }
                    if (count == 2) break;
                    if (_pair2.Key.Equals(key))
                    {
                        return _pair2.Value;
                    }
                    if (count == 3) break;
                    if (_pair3.Key.Equals(key))
                    {
                        return _pair3.Value;
                    }
                    if (count == 4) break;
                    if (_pair4.Key.Equals(key))
                    {
                        return _pair4.Value;
                    }
                    if (count == 5) break;
                    if (_pair5.Key.Equals(key))
                    {
                        return _pair5.Value;
                    }
                    if (count == 6) break;
                    if (_pair6.Key.Equals(key))
                    {
                        return _pair6.Value;
                    }
                    if (count == 7) break;
                    if (_pair7.Key.Equals(key))
                    {
                        return _pair7.Value;
                    }
                    if (count == 8) break;
                    if (_pair8.Key.Equals(key))
                    {
                        return _pair8.Value;
                    }
                    if (count == 9) break;
                    if (_pair9.Key.Equals(key))
                    {
                        return _pair9.Value;
                    }
                    if (count == 10) break;
                    if (_pair10.Key.Equals(key))
                    {
                        return _pair10.Value;
                    }
                    if (count == 11) break;
                    if (_pair11.Key.Equals(key))
                    {
                        return _pair11.Value;
                    }
                    if (count == 12) break;
                    if (_pair12.Key.Equals(key))
                    {
                        return _pair12.Value;
                    }
                    if (count == 13) break;
                    if (_pair13.Key.Equals(key))
                    {
                        return _pair13.Value;
                    }
                    if (count == 14) break;
                    if (_pair14.Key.Equals(key))
                    {
                        return _pair14.Value;
                    }
                    break;
                }
                throw new KeyNotFoundException();
            }
        }

        public IEnumerable<TKey> Keys => _fallbackDictionary.Keys;

        public IEnumerable<TValue> Values => _fallbackDictionary.Values;

        public int Count => _fallbackDictionary.Count;

        public bool ContainsKey(TKey key)
        {
            while (_hasValue)
            {
                if (_useFallbackDictionary)
                {
                    return _fallbackDictionary.ContainsKey(key);
                }
                int count = _count;
                if (_pair0.Key.Equals(key))
                {
                    return true;
                }
                if (count == 1) break;
                if (_pair1.Key.Equals(key))
                {
                    return true;
                }
                if (count == 2) break;
                if (_pair2.Key.Equals(key))
                {
                    return true;
                }
                if (count == 3) break;
                if (_pair3.Key.Equals(key))
                {
                    return true;
                }
                if (count == 4) break;
                if (_pair4.Key.Equals(key))
                {
                    return true;
                }
                if (count == 5) break;
                if (_pair5.Key.Equals(key))
                {
                    return true;
                }
                if (count == 6) break;
                if (_pair6.Key.Equals(key))
                {
                    return true;
                }
                if (count == 7) break;
                if (_pair7.Key.Equals(key))
                {
                    return true;
                }
                if (count == 8) break;
                if (_pair8.Key.Equals(key))
                {
                    return true;
                }
                if (count == 9) break;
                if (_pair9.Key.Equals(key))
                {
                    return true;
                }
                if (count == 10) break;
                if (_pair10.Key.Equals(key))
                {
                    return true;
                }
                if (count == 11) break;
                if (_pair11.Key.Equals(key))
                {
                    return true;
                }
                if (count == 12) break;
                if (_pair12.Key.Equals(key))
                {
                    return true;
                }
                if (count == 13) break;
                if (_pair13.Key.Equals(key))
                {
                    return true;
                }
                if (count == 14) break;
                if (_pair14.Key.Equals(key))
                {
                    return true;
                }
                break;
            }
            return false;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _fallbackDictionary.GetEnumerator();

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (_hasValue)
            {
                if (_useFallbackDictionary)
                {
                    return _fallbackDictionary.TryGetValue(key, out value);
                }
                var count = _count;
                value = default;
                if (_pair0.Key.Equals(key))
                {
                    value = _pair0.Value;
                    return true;
                }
                if (count == 1) return false;
                if (_pair1.Key.Equals(key))
                {
                    value = _pair1.Value;
                    return true;
                }
                if (count == 2) return false;
                if (_pair2.Key.Equals(key))
                {
                    value = _pair2.Value;
                    return true;
                }
                if (count == 3) return false;
                if (_pair3.Key.Equals(key))
                {
                    value = _pair3.Value;
                    return true;
                }
                if (count == 4) return false;
                if (_pair4.Key.Equals(key))
                {
                    value = _pair4.Value;
                    return true;
                }
                if (count == 5) return false;
                if (_pair5.Key.Equals(key))
                {
                    value = _pair5.Value;
                    return true;
                }
                if (count == 6) return false;
                if (_pair6.Key.Equals(key))
                {
                    value = _pair6.Value;
                    return true;
                }
                if (count == 7) return false;
                if (_pair7.Key.Equals(key))
                {
                    value = _pair7.Value;
                    return true;
                }
                if (count == 8) return false;
                if (_pair8.Key.Equals(key))
                {
                    value = _pair8.Value;
                    return true;
                }
                if (count == 9) return false;
                if (_pair9.Key.Equals(key))
                {
                    value = _pair9.Value;
                    return true;
                }
                if (count == 10) return false;
                if (_pair10.Key.Equals(key))
                {
                    value = _pair10.Value;
                    return true;
                }
                if (count == 11) return false;
                if (_pair11.Key.Equals(key))
                {
                    value = _pair11.Value;
                    return true;
                }
                if (count == 12) return false;
                if (_pair12.Key.Equals(key))
                {
                    value = _pair12.Value;
                    return true;
                }
                if (count == 13) return false;
                if (_pair13.Key.Equals(key))
                {
                    value = _pair13.Value;
                    return true;
                }
                if (count == 14) return false;
                if (_pair14.Key.Equals(key))
                {
                    value = _pair14.Value;
                    return true;
                }
                return false;
            }
            value = default;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => _fallbackDictionary.GetEnumerator();
    }
}
