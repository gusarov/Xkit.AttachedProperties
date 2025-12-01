using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace Xkit.AttachedProperties
{

	public static class State
	{
		private class MultiKeyNode : IndexedMultiDictionaryDynamicObject // you can tweak by changing parent type here, e.g. only string keys, or dynamic binding, or 2 IDictionary<,> interfaces
		{
			//// one ConditionalWeakTable is declared in parent class for properties
			//// this one is second ConditionalWeakTable for multikey navigation
			private readonly ConditionalWeakTable<object, MultiKeyNode> _children = new();

			public MultiKeyNode ThenFor(object key) => _children.GetOrCreateValue(key);
		}

		private static readonly MultiKeyNode _rootKey = new MultiKeyNode();

		public static dynamic AttachedTo(object weakKey) => AttachedTo(_rootKey.ThenFor(weakKey));

		public static dynamic AttachedTo(params IEnumerable<object> weakKeys) => AttachedTo(_rootKey, weakKeys);

		internal static dynamic AttachedTo(object firstWeakKey, params IEnumerable<object> weakKeys) => AttachedTo(_rootKey.ThenFor(firstWeakKey), weakKeys);

		private static dynamic AttachedTo(MultiKeyNode current, params IEnumerable<object> weakKeys)
		{
			if (weakKeys is IReadOnlyList<object> keysList)
			{
				for (int i = 0, m = keysList.Count; i < m; i++)
				{
					current = current.ThenFor(keysList[i]);
				}
				return current;
			}
			else
			{
				foreach (var item in weakKeys)
				{
					current = current.ThenFor(item);
				}
				return current;
			}
		}

#if NET || NETSTANDARD2_1
		public static dynamic AttachedTo(params ReadOnlySpan<object> weakKeys) => AttachedFor(_rootKey, weakKeys);

		internal static dynamic AttachedTo(object firstWeakKey, params ReadOnlySpan<object> weakKeys) => AttachedFor(_rootKey.ThenFor(firstWeakKey), weakKeys);

		private static dynamic AttachedFor(MultiKeyNode current, params ReadOnlySpan<object> weakKeys)
		{
			for (int i = 0, m = weakKeys.Length; i < m; i++)
			{
				current = current.ThenFor(weakKeys[i]);
			}
			return current;
		}
#endif

		/*
		private class StringIndexedDynamicObject : DynamicObject, IDictionary<string, object?>
		{
			Dictionary<string, object?> _data = new Dictionary<string, object?>();

			public object? this[string key]
			{
				get => _data[key];
				set => _data[key] = value;
			}

			public override bool TryGetMember(GetMemberBinder binder, out object? result)
			{
				result = this[binder.Name];
				return true; // othervise it throws on dynamic lookup
			}

			public override bool TrySetMember(SetMemberBinder binder, object? value)
			{
				this[binder.Name] = value;
				return true;
			}

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
			{
				if (indexes.Length == 1)
				{
					if (indexes[0] == null)
					{
						throw new Exception($"Can't bind index. Key cannot be null");
					}
					if (indexes[0] is string key)
					{
						result = this[key];
						return true;
					}
					throw new Exception($"Can't bind index. Expected type of argument: string, Actual: {indexes[0].GetType().Name}");
				}
				throw new Exception($"Can't bind index. Expected count of parameters: 1, Actual: {indexes.Length}");
			}

			public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object? value)
			{
				if (indexes.Length == 1)
				{
					if (indexes[0] == null)
					{
						throw new Exception($"Can't bind index. Key cannot be null");
					}
					if (indexes[0] is string key)
					{
						this[key] = value;
						return true;
					}
					throw new Exception($"Can't bind index. Expected type of argument: string, Actual: {indexes[0].GetType().Name}");
				}
				throw new Exception($"Can't bind index. Expected count of parameters: 1, Actual: {indexes.Length}");
			}

			public ICollection<string> Keys => _data.Keys;

			public ICollection<object?> Values => _data.Values;

			int ICollection<KeyValuePair<string, object?>>.Count => _data.Count;

			bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => false;

			public void Add(string key, object? value)
			{
				_data.Add(key, value);
			}

			void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item)
			{
				((ICollection<KeyValuePair<string, object?>>)_data).Add(item);
			}

			void ICollection<KeyValuePair<string, object?>>.Clear()
			{
				throw new NotSupportedException();
			}

			bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item)
			{
				return ((ICollection<KeyValuePair<string, object?>>)_data).Contains(item);
			}

			public bool ContainsKey(string key)
			{
				return ((IDictionary<string, object?>)_data).ContainsKey(key);
			}

			public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
			{
				((ICollection<KeyValuePair<string, object?>>)_data).CopyTo(array, arrayIndex);
			}

			public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
			{
				return ((IEnumerable<KeyValuePair<string, object?>>)_data).GetEnumerator();
			}

			public bool Remove(string key)
			{
				return ((IDictionary<string, object?>)_data).Remove(key);
			}

			public bool Remove(KeyValuePair<string, object?> item)
			{
				return ((ICollection<KeyValuePair<string, object?>>)_data).Remove(item);
			}

			public bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value)
			{
				return ((IDictionary<string, object?>)_data).TryGetValue(key, out value);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable)_data).GetEnumerator();
			}
		}

		private class IndexedDynamicObject : DynamicObject, IDictionary<object, object?>
		{
			private ConditionalWeakTable<object, object?> _dictionary = new();

			ICollection<object> IDictionary<object, object?>.Keys => _dictionary.Select(_dictionary => _dictionary.Key).ToArray();

			ICollection<object> IDictionary<object, object?>.Values => _dictionary.Select(_dictionary => _dictionary.Value).ToArray();

			int ICollection<KeyValuePair<object, object?>>.Count => _dictionary.Count();

			bool ICollection<KeyValuePair<object, object?>>.IsReadOnly => false;

			public object? this[object key]
			{
				get
				{
					_dictionary.TryGetValue(key, out var result);
					return result;
				}
				set
				{
					if (value == null)
					{
						_dictionary.Remove(key);
					}
					else
					{
						_dictionary.AddOrUpdate(key, value);
					}
				}
			}

			public override bool TryGetMember(GetMemberBinder binder, out object? result)
			{
				result = this[binder.Name];
				return true; // othervise it throws on dynamic lookup
			}

			public override bool TrySetMember(SetMemberBinder binder, object? value)
			{
				this[binder.Name] = value;
				return true;
			}

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
			{
				result = this[indexes[0]];
				return true;
			}

			public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object? value)
			{
				this[indexes[0]] = value;
				return true;
			}

			void IDictionary<object, object?>.Add(object key, object value)
			{
				this[key] = value;
			}

			bool IDictionary<object, object?>.ContainsKey(object key)
			{
				throw new NotSupportedException("Very ineficiente");
			}

			bool IDictionary<object, object?>.Remove(object key)
			{
				return _dictionary.Remove(key);
			}

			bool IDictionary<object, object?>.TryGetValue(object key, out object value)
			{
				return _dictionary.TryGetValue(key, out value);
			}

			void ICollection<KeyValuePair<object, object?>>.Add(KeyValuePair<object, object?> item)
			{
				this[item.Key] = item.Value;
			}

			void ICollection<KeyValuePair<object, object?>>.Clear()
			{
				throw new NotSupportedException("Esto es injusto");
				_dictionary.Clear();
			}

			bool ICollection<KeyValuePair<object, object?>>.Contains(KeyValuePair<object, object?> item)
			{
				return _dictionary.TryGetValue(item.Key, out var value) && value == item.Value;
			}

			void ICollection<KeyValuePair<object, object?>>.CopyTo(KeyValuePair<object, object?>[] array, int arrayIndex)
			{
				throw new NotImplementedException();
			}

			bool ICollection<KeyValuePair<object, object?>>.Remove(KeyValuePair<object, object?> item)
			{
				return _dictionary.Remove(item.Key);
			}

			IEnumerator<KeyValuePair<object, object?>> IEnumerable<KeyValuePair<object, object?>>.GetEnumerator()
			{
				return ((IEnumerable<KeyValuePair<object, object?>>)_dictionary).GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable)_dictionary).GetEnumerator();
			}
		}

		*/
		private class IndexedMultiDictionaryDynamicObject : DynamicObject, IDictionary<object, object?>, IDictionary<string, object?>
		{
			ConcurrentDictionary<object, object?> _data = new();
			IDictionary<object, object?> _dataDict => _data;

			#region DynamicObject

			public override bool TryGetMember(GetMemberBinder binder, out object? result)
			{
				result = this[binder.Name];
				return true; // othervise it throws on dynamic lookup
			}

			public override bool TrySetMember(SetMemberBinder binder, object? value)
			{
				this[binder.Name] = value;
				return true;
			}

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
			{
				if (indexes.Length == 1)
				{
					if (indexes[0] == null)
					{
						throw new Exception($"Can't bind index. Key cannot be null");
					}
					result = this[indexes[0]];
					return true;
				}
				throw new Exception($"Can't bind index. Expected count of parameters: 1, Actual: {indexes.Length}");
			}

			public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object? value)
			{
				if (indexes.Length == 1)
				{
					if (indexes[0] == null)
					{
						throw new Exception($"Can't bind index. Key cannot be null");
					}
					this[indexes[0]] = value;
					return true;
				}
				throw new Exception($"Can't bind index. Expected count of parameters: 1, Actual: {indexes.Length}");
			}

			#endregion

			public object? this[object key] { get => _data[key]; set => _data[key] = value; }
			object? IDictionary<string, object?>.this[string key] { get => _data[key]; set => _data[key] = value; }

			public ICollection<object> Keys => _dataDict.Keys;

			public ICollection<object?> Values => _dataDict.Values;

			public int Count => _data.Count;

			public bool IsReadOnly => false;

			// ICollection<string> IDictionary<string, object?>.Keys => throw new NotSupportedException("Do not consider keys are strings, you might miss some of objects. If you really want only string keys - get them as ((IDictionary<object, object?>)attached).Keys.OfType<string>()");
			ICollection<string> IDictionary<string, object?>.Keys => _data.Keys.OfType<string>().ToArray();

			int ICollection<KeyValuePair<string, object?>>.Count => _data.Count;

			bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => false;

			public void Add(object key, object? value)
			{
				_dataDict.Add(key, value);
			}

			public void Add(KeyValuePair<object, object?> keyValuePair)
			{
				_dataDict.Add(keyValuePair);
			}

			public void Clear()
			{
				throw new NotSupportedException("It is illegal to clear all attached state because you might not own some of it.");
			}

			public bool Contains(KeyValuePair<object, object?> item)
			{
				return _dataDict.Contains(item);
			}

			public bool ContainsKey(object key)
			{
				return _dataDict.ContainsKey(key);
			}

			public void CopyTo(KeyValuePair<object, object?>[] array, int arrayIndex)
			{
				_dataDict.CopyTo(array, arrayIndex);
			}

			public IEnumerator<KeyValuePair<object, object?>> GetEnumerator()
			{
				return _dataDict.GetEnumerator();
			}

			public bool Remove(object key)
			{
				return _dataDict.Remove(key);
			}

			public bool Remove(KeyValuePair<object, object?> item)
			{
				return _dataDict.Remove(item);
			}

#if NET || NETSTANDARD2_1
			public bool TryGetValue(object key, [MaybeNullWhen(false)] out object? value)
			{
				return _dataDict.TryGetValue(key, out value);
			}
#else
			public bool TryGetValue(object key, out object? value)
			{
				return _dataDict.TryGetValue(key, out value);
			}
#endif

			void IDictionary<string, object?>.Add(string key, object? value)
			{
				_dataDict.Add(key, value);
			}

			void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> keyValuePair)
			{
				_dataDict.Add(new KeyValuePair<object, object?>(keyValuePair.Key, keyValuePair.Value));
			}

			void ICollection<KeyValuePair<string, object?>>.Clear()
			{
				Clear();
			}

			bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> keyValuePair)
			{
				return _dataDict.TryGetValue(keyValuePair.Key, out var value) && EqualityComparer<object?>.Default.Equals(value, keyValuePair.Value);
			}

			bool IDictionary<string, object?>.ContainsKey(string key)
			{
				return _dataDict.ContainsKey(key);
			}

			void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
			{
				// need to work with IEnumerator<KeyValuePair<string, object?>>
				throw new NotImplementedException();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable)_data).GetEnumerator();
			}

			IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
			{
				foreach (var item in _data)
				{
					if (item.Key is string keyStr)
					{
						yield return new KeyValuePair<string, object?>(keyStr, item.Value);
					}
				}
			}

			bool IDictionary<string, object?>.Remove(string key)
			{
				return _dataDict.Remove(key);
			}

			bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item)
			{
				// need to work with equality comparer of actually stored value
				throw new NotImplementedException();
			}

			bool IDictionary<string, object?>.TryGetValue(string key, out object? value)
			{
				return _dataDict.TryGetValue(key, out value);
			}
		}
	}

	/*


	interface IAttachedProperties : IDynamicMetaObjectProvider, IDictionary<string, object?>
	{

	}

	internal class EquatableWeakReference : WeakReference, IEquatable<EquatableWeakReference>
	{
		readonly int _hashCode;

		public EquatableWeakReference(object obj) : base(obj)
		{
			_hashCode = obj.GetHashCode();
		}

		bool IEquatable<EquatableWeakReference>.Equals(EquatableWeakReference other)
		{
			return Equals(other);
		}

		public override bool Equals(object other)
		{
			if (ReferenceEquals(other, null))
			{
				return false;
			}
			if (other.GetHashCode() != _hashCode)
			{
				return false;
			}
			var aliveTarget = Target;
			return ReferenceEquals(other, this) || ReferenceEquals(other, aliveTarget);
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}
	}

	internal class EquatableWeakReferenceKeys : IEquatable<EquatableWeakReferenceKeys>
	{
		readonly int _hashCode;
		WeakReference[] _keys;

		public EquatableWeakReferenceKeys(IReadOnlyList<object> keys)
		{
			ArgumentNullException.ThrowIfNull(keys);
			_keys = new WeakReference[keys.Count];
			HashCode hashCode = default;
			for (int i = 0; i < keys.Count; i++)
			{
				_keys[i] = new WeakReference(keys[i]);
				hashCode.Add(keys[i]);
			}
			_hashCode = hashCode.ToHashCode();
		}

		bool IEquatable<EquatableWeakReferenceKeys>.Equals(EquatableWeakReferenceKeys? other)
		{
			return Equals(other);
		}

		public override bool Equals(object? other)
		{
			if (ReferenceEquals(other, null))
			{
				return false;
			}
			if (other.GetHashCode() != _hashCode)
			{
				return false;
			}
			if (ReferenceEquals(other, this)) // collector will work because it brings exact same instance
			{
				return true;
			}
			if (other is EquatableWeakReferenceKeys otherKeys) // but others considered equal only while alive
			{
				if (otherKeys._keys.Length != _keys.Length)
				{
					return false;
				}
				for (int i = 0; i < _keys.Length; i++)
				{
					var targetA = _keys[i].Target;
					var targetB = otherKeys._keys[i].Target;
					if (ReferenceEquals(targetA, null))
					{
						return false;
					}
					if (ReferenceEquals(targetB, null))
					{
						return false;
					}
					if (!Equals(targetA, targetB))
					{
						return false;
					}
				}
			}
			return false;
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}
	}

	internal sealed class WeakKeyComparer : IEqualityComparer<EquatableWeakReference>
	{
		public static WeakKeyComparer Comparer { get; } = new WeakKeyComparer();

		WeakKeyComparer()
		{

		}

		bool IEqualityComparer<EquatableWeakReference>.Equals(EquatableWeakReference? x, EquatableWeakReference? y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}
			if (ReferenceEquals(x, null))
			{
				return ReferenceEquals(y, null);
			}
			if (ReferenceEquals(y, null))
			{
				return false;
			}
			if (x.GetHashCode() != y.GetHashCode())
			{
				return false;
			}
			//			var wx = x as WeakReference;
			//			var wy = y as WeakReference;

			var ax = x.Target;
			var ay = y.Target;

			if (ReferenceEquals(ax, null))
			{
				return ReferenceEquals(ay, null);
			}

			if (ReferenceEquals(ay, null))
			{
				return false;
			}

			return Equals(ax, ay);
		}

		int IEqualityComparer<EquatableWeakReference>.GetHashCode(EquatableWeakReference ewr)
		{
			return ewr.GetHashCode();
		}
	}

	internal class ObjectLifeTracker
	{
		internal ConcurrentBag<EquatableWeakReferenceKeys>? _keys;

		public ObjectLifeTracker()
		{
			GC.SuppressFinalize(this);
		}

		~ObjectLifeTracker()
		{
			// AttachedProperties.CleanupCollectedEntries(Key);
			foreach (var key in _keys ?? [])
			{
				AttachedProperties.CleanupCollectedEntries(key);
			}
		}

		public void RegisterKey(EquatableWeakReferenceKeys ewr)
		{
			if (_keys == null)
			{
				lock (this)
				{
					if (_keys == null)
					{
						_keys = new ConcurrentBag<EquatableWeakReferenceKeys>();
						GC.ReRegisterForFinalize(this); // only once we have keys to track
					}
				}
			}
			_keys.Add(ewr);
		}
	}
	*/

	namespace Extensions.Attached
	{
		public static class Extension
		{
			extension<T>(T obj) where T : notnull
			{
				public dynamic Attached => State.AttachedTo(obj);
			}
		}
	}

	namespace Extensions.GetAttached
	{
		public static class Extension
		{
			public static dynamic GetAttached<T>(this T obj) where T : notnull => State.AttachedTo(obj);
		}
	}

	namespace Extensions.AttachedTo
	{
		public static class Extension
		{
			extension<T>(T obj) where T : notnull
			{
#if NET || NETSTANDARD2_1
				public dynamic AttachedTo(params ReadOnlySpan<object> weakKeys) => State.AttachedTo(obj, weakKeys);
#endif
				public dynamic AttachedTo(params IEnumerable<object> weakKeys) => State.AttachedTo(obj, weakKeys);
			}
		}
	}
}

