using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace Xkit;

public static class AttachedProperties
{
	private static ConditionalWeakTable<object, /*IndexedDynamicObject*/StringIndexedDynamicObject> _attachedProperties = new();

	extension<T>(T obj)	where T : notnull
	{
		public dynamic Attached => GetAttachedDictionary(obj);
	}

	/*
	public static dynamic Attached(this object obj)
	{
		return _attachedProperties.GetOrCreateValue(obj);
	}
	*/

	private static StringIndexedDynamicObject GetAttachedDictionary(object obj)
	{
		return _attachedProperties.GetOrCreateValue(obj);
	}

	internal static IEnumerable<object> GetExtendedObjectsAlive()
	{
		return _attachedProperties.Select(pair => pair.Key);
	}

	private class StringIndexedDynamicObject : DynamicObject, IAttachedProperties, IDictionary<string, object?>
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

}

public interface IAttachedProperties : IDynamicMetaObjectProvider, IDictionary<string, object?>
{

}
