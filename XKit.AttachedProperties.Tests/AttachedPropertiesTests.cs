using System.Data;
using System.Diagnostics;
using XKit;

using XKit.AttachedProperties;
using XKit.AttachedProperties.Extensions.Attached;
using XKit.AttachedProperties.Extensions.AttachedTo;

namespace XKit.Tests;

[NonParallelizable]
public class AttachedPropertiesTests
{
	List<WeakReference> _weakReferences = new();

	T Track<T>(T obj)
	{
		_weakReferences.Add(new WeakReference(obj));
		return obj;
	}

	[SetUp]
	public void Setup()
	{
	}

	[TearDown]
	public void TearDown()
	{
		try
		{
			Assert.Multiple(() =>
			{
				// int maxAttemptsEncountered = 0; // it is lying because we are in a loop, so each item had a gc
				foreach (var item in _weakReferences)
				{
					for (int i = 1; item.IsAlive && (i < 5 || Debugger.IsAttached); i++)
					{
						// maxAttemptsEncountered = Math.Max(maxAttemptsEncountered, i);

						GC.Collect();
						GC.WaitForPendingFinalizers();
						if (Debugger.IsAttached)
						{
							Thread.Sleep(100);
						}
						// if (i % 10 == 0)
						// {
						// 	Console.WriteLine(); // breakpoint
						// }
					}
					Assert.That(item.IsAlive, Is.False, $"Object is not dead: {item.Target?.GetType()?.Name}: {item.Target}");
				}
				// Console.WriteLine("maxAttemptsEncountered = " + maxAttemptsEncountered);
			});
		}
		catch
		{
			/*
			Console.WriteLine("==== objects ====");
			foreach (var item in AttachedProperties.GetExtendedObjectsAlive())
			{
				Console.WriteLine($"{item.GetType().Name}: {item}");
			}

			Console.WriteLine("==== values ====");
			foreach (var item in AttachedProperties.GetExtendedDataAlive())
			{
				bool any = false;
				foreach (var subItem in item)
				{
					any = true;
					Console.WriteLine($"{subItem.Key} = {subItem.Value?.GetType()?.Name}: {subItem.Value}");
				}
				if (any)
				{
					Console.WriteLine("----");
				}
			}
			*/
			throw;
		}
	}

	[Test]
	public void Should_10_add_extra_state_to_object_as_dictionary_string()
	{
		var obj = Track(new SampleEntity { Name = "Test" });
		var dic = (IDictionary<string, object>)obj.Attached;
		dic["Country"] = "Canada";
		dic["Age"] = 38;

		Track(dic);

		TryToCollect();

		Assert.That(dic["Country"], Is.EqualTo("Canada"));
		Assert.That(dic["Age"], Is.EqualTo(38));
	}

	[Test]
	public void Should_11_add_extra_state_to_object_as_dynamic_indexer()
	{
		var obj = Track(new SampleEntity { Name = "Test" });

		obj.Attached["Country"] = "Canada";
		obj.Attached["Age"] = 38;

		Track(obj.Attached);

		TryToCollect();

		Assert.That(obj.Attached["Country"], Is.EqualTo("Canada"));
		Assert.That(obj.Attached["Age"], Is.EqualTo(38));
	}

	[Test]
	public void Should_13_add_extra_state_to_object_as_dynamic_properties()
	{
		var obj = Track(new SampleEntity { Name = "Test" });
		obj.Attached.Country = "Canada";
		obj.Attached.Age = 38;
		Assert.That(obj.Attached.Country, Is.EqualTo("Canada"));
		Assert.That(obj.Attached.Age, Is.EqualTo(38));
	}

	[Test]
	public void Should_20_add_extra_state_to_object_as_dictionary_object()
	{
		var obj = Track(new SampleEntity { Name = "Test" });
		var dic = Track((IDictionary<object, object>)obj.Attached);
		obj.Attached["Country"] = "Canada";
		obj.Attached["Age"] = 38;

		var key1 = Track(new SampleReferenceKey { KeyData1 = "1", KeyData2 = "val" }); // define a key object
		obj.Attached[key1] = 777;
		var key2 = Track(new SampleReferenceKey { KeyData1 = "1", KeyData2 = "ref" }); // define a key object
		obj.Attached[key2] = Track(new SampleExtendedValue { Extra = "data3" });

		TryToCollect();

		Assert.That(obj.Attached["Country"], Is.EqualTo("Canada"));
		Assert.That(obj.Attached["Age"], Is.EqualTo(38));

		Assert.That(obj.Attached[key1], Is.EqualTo(777));
		Assert.That(obj.Attached[key2].Extra, Is.EqualTo("data3"));
	}

	[Test]
	public void Should_21_add_extra_state_to_object_as_dynamic_indexer_with_object_key()
	{
		var obj = Track(new SampleEntity { Name = "Test" });
		obj.Attached["Country"] = "Canada";
		obj.Attached["Age"] = 38;
		Track(obj.Attached);

		var key1 = Track(new SampleReferenceKey { KeyData1 = "1", KeyData2 = "val" }); // define a key object
		obj.Attached[key1] = 777;
		var key2 = Track(new SampleReferenceKey { KeyData1 = "1", KeyData2 = "ref" }); // define a key object
		obj.Attached[key2] = Track(new SampleExtendedValue { Extra = "data3" });

		TryToCollect();

		Assert.That(obj.Attached["Country"], Is.EqualTo("Canada"));
		Assert.That(obj.Attached["Age"], Is.EqualTo(38));

		Assert.That(obj.Attached[key1], Is.EqualTo(777));
		Assert.That(obj.Attached[key2].Extra, Is.EqualTo("data3"));
	}

	[Test]
	public void Should_15_not_collect_extra_state_while_object_is_alive()
	{
		SampleEntity parent = new SampleEntity { Name = "Parent" };

		WeakReference<SampleEntity> weakChild;
		void Inner()
		{
			var objChild = new SampleEntity { Name = "Child" };
			weakChild = new WeakReference<SampleEntity>(objChild);

			parent.Attached.Child = objChild;

			Assert.That(parent.Attached.Child.Name, Is.EqualTo("Child"));

			objChild = null;
		}
		Inner();

		TryToCollect();

		Assert.That(weakChild.TryGetTarget(out var _), Is.True);
		Assert.That(parent.Attached.Child.Name, Is.EqualTo("Child"));
	}

	[Test]
	public void Should_20_collect_extra_state_when_parent_collected()
	{
		WeakReference weakSampleEntity;
		WeakReference weakSampleValue;

		void Inner()
		{
			var objEntity = Track(new SampleEntity { Name = "Parent" });
			weakSampleEntity = new WeakReference(objEntity);

			var objValue = Track(new SampleExtendedValue { Extra = "Child" });
			weakSampleValue = new WeakReference(objValue);

			objEntity.Attached.Child = objValue;

			Assert.That(objEntity.Attached.Child.Extra, Is.EqualTo("Child"));

			objEntity = null;
		}
		Inner();

		AssertDead(weakSampleEntity);
		AssertDead(weakSampleValue);
	}


	[Test]
	public void Should_30_associate_state_to_the_set_of_weak_references()
	{
		var objEntity = Track(new SampleEntity { Name = "Parent" });

		WeakReference wrKey2;
		WeakReference wrValue3;

		void Inner()
		{
			objEntity.Attached.Abc = 1;
			var sampleService = new SampleService();
			wrKey2 = new WeakReference(sampleService);

			var attachedWithService = State.AttachedTo(objEntity, sampleService);

			var value = Track(new SampleExtendedValue() { Extra = "Child" });
			wrValue3 = new WeakReference(value);
			attachedWithService.Child = value;

			TryToCollect();

			Assert.That(attachedWithService.Child.Extra, Is.EqualTo("Child"));
			Assert.That(objEntity.AttachedTo(sampleService!).Child.Extra, Is.EqualTo("Child"));

			sampleService = null;
		}
		Inner();

		AssertDead(wrKey2);
		AssertDead(wrValue3); // part of attached state is released, and there is no way to check because wrKey2 also released

		Assert.That(objEntity.Attached.Abc, Is.EqualTo(1));
		// Assert.That(objEntity.Attached.Child.Extra, Is.EqualTo("Child"));

		/*
		Console.WriteLine("==== objects ====");
		foreach (var item in AttachedProperties.GetExtendedObjectsAlive())
		{
			Console.WriteLine($"{item.GetType().Name}: {item}");
		}

		Console.WriteLine("==== values ====");
		foreach (var item in AttachedProperties.GetExtendedDataAlive())
		{
			bool any = false;
			foreach (var subItem in item)
			{
				any = true;
				Console.WriteLine($"{subItem.Key} = {subItem.Value?.GetType()?.Name}: {subItem.Value}");
			}
			if (any)
			{
				Console.WriteLine("----");
			}
		}
		*/
	}

	#region Helpers

	private static void TryToCollect()
	{
		for (int i = 0; i < 4; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
	}

	private static void AssertDead(WeakReference weakReference)
	{
		int attempts = 3;
		while (weakReference.IsAlive)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			if (--attempts == 0) throw new Exception("It is not dying");
		}
	}

	private static void AssertDead<T>(WeakReference<T> weakReference)
		where T : class
	{
		int attempts = 3;
		while (weakReference.TryGetTarget(out var t))
		{
			t = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			if (--attempts == 0) throw new Exception("It is not dying");
		}
	}

	#endregion
}

public class SampleEntity
{
	public required string Name { get; init; }
}

public class SampleService
{

}

public class SampleReferenceKey
{
	public required string KeyData1 { get; init; }
	public required string KeyData2 { get; init; }
}
public class SampleExtendedValue
{
	public required string Extra { get; init; }
}
