# Attached Properties for .NET

Add state to any existing object. Control the lifetime of that state with strong or weak references.

- Attach arbitrary values (including references) to any object at runtime.
- Use simple dynamic syntax: `obj.Attached.SomeProperty = value`.
- Support composite keys for strong: `obj.Attached[new Key(...)] = value` and weak `obj.AttachedFor(svc1, svc2).Some = value`.
- Use weak keys to scope state by service, request, or any object lifetime.

> Targets: .NET Standard 2.0, .NET Standard 2.1, .NET 8, .NET 9, .NET 10

## Quick Start

### Test Model

```cs
	class MyClass
	{
		public string Name { get; set;  }
	}
	var myObject = new MyClass { Name = "Test" };
```

## Strong References

### Add state to attached properties:

```cs
myObject.Attached.Age = 25;

// Explanation:
// This value will live as long as myObject lives. myObject owns the state from lifetime perspective.
// The value 25 is associated by string key "Age" via dynamic binding, so all of this would work equally fine:

Console.WriteLine(myObject.Attached.Age);
Console.WriteLine(myObject.Attached["Age"]);
Console.WriteLine(((IDictionary<string, object>)myObject.Attached)["Age"]);
```

### Add references to attached properties:

```cs
var child = new MyClass { Name = "Child" };
myObject.Attached.Child = child;

// Explanation:
// now even if you remove all references to child, it will live as long as myObject lives because it also owns the child now and keeps strong reference.
```

### Add state by composite or reference key

Does not matter if it value type or reference, think about this as about Dictionary<object, object>. What matters is that the key is unique, comparable and the reference to key is strong.

```cs
record /*struct*/ Key(string Name, decimal version, MyClass relation);
myObject.Attached[new Key("Data", 1.0m, child)] = "Some data";

// Explanation:
// now even if you remove all references to child or Key, it will live as long as myObject lives because it also owns the key and child now and keeps strong reference.
```

### Conclusion

If you think about it, everything is strongly referenced except the myObject itself. So the state will live as long as myObject lives, and myObject helds only Weak Reference for that purposes.

If your objects are ad-hock (e.g. Scoped Service), and you want to keep state only as long as scope lives, avoid keeping strong reference to them, you need techniques described below.

## Weak References

### Extend a state per service/object basis by a weak key

```cs
class MyService
{
	public void Handle(MyClass item)
	{
		item.AttachedTo(this).Age = 30; // AttachedTo(this) is the way to go!
	}
}

// Explanation:
// now if you remove all references to MyService instance, and let it be collected, the state will be partially removed as well because "this" is considered weak key.

item.AttachedTo(this, new object()).Age = 30;

// Explanation:
// you can have many weak keys per attached state. But all of them, including the item itself are weakReferences. So in this example the state will be lost in a first GC round, because noone hold new object.
```

### Conclusion

If you think about it, attached state is like Dictionary<object, object> but they are different for every key combination. It is like if every key combination brings it's unique dictionary. And every key is equally independed weak reference. This way it does not matter who lives longer, the state will be partially removed as soon as any of the keys are collected.

To highlight that idea and philosofy even more, check out the root static method:

```cs
Xkit.AttachedProperties.State.AttachedTo(params IEnumerable<object> weakKeys)
```

This explains that `.Attached` extension property is just a convention for State.AttachedTo(this)

### Important notes:

Another important hint is that it does not really matter what order GetFor keys are passed. So

item.AttachedTo(worker, ledger)
item.AttachedTo(ledger, worker)
worker.AttachedTo(ledger, item)

This all produces the same attached state dictionary that lives as long as all keys alive. And this never extends lifetime of any of the keys.

Now if you intentionally want to extend lifetime of some key, you should assign it explicitly to owner or owners:

item.Attached(worker).Ledger = ledger; // now lifetime of ledger is extended by item and worker. Item and worker are both weakKeys and they own the "Ledger" value attached to them.
