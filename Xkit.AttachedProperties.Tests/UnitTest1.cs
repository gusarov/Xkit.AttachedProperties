using Xkit;

namespace Xkit.AttachedProperties.Tests;

public class AttachedPropertiesTests
{
	[SetUp]
	public void Setup()
	{
	}

	[Test]
	public void Should_add_extra_state_to_object_as_dictionary_string()
	{
		var obj = new SampleEntity { Name = "Test" };
		var dic = (IDictionary<string, object>)obj.Attached;
		obj.Attached["Country"] = "Canada";
		obj.Attached["Age"] = 38;
		Assert.That(obj.Attached["Country"], Is.EqualTo("Canada"));
		Assert.That(obj.Attached["Age"], Is.EqualTo(38));
	}

	[Test]
	public void Should_add_extra_state_to_object_as_dynamic_indexer()
	{
		var obj = new SampleEntity { Name = "Test" };
		obj.Attached["Country"] = "Canada";
		obj.Attached["Age"] = 38;
		Assert.That(obj.Attached["Country"], Is.EqualTo("Canada"));
		Assert.That(obj.Attached["Age"], Is.EqualTo(38));
	}

	/*
	[Test]
	public void Should_add_extra_state_to_object_as_dictionary_object()
	{
		Assert.Pass();
	}
	*/

	[Test]
	public void Should_add_extra_state_to_object_as_dynamic_properties()
	{
		var obj = new SampleEntity { Name = "Test" };
		obj.Attached.Country = "Canada";
		obj.Attached.Age = 38;
		Assert.That(obj.Attached.Country, Is.EqualTo("Canada"));
		Assert.That(obj.Attached.Age, Is.EqualTo(38));
	}

	[Test]
	public void Should_not_collect_extra_state_while_object_is_alive()
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

		for (int i = 0; i < 4; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		Assert.That(weakChild.TryGetTarget(out var _), Is.True);
		Assert.That(parent.Attached.Child.Name, Is.EqualTo("Child"));
	}

	[Test]
	public void Should_collect_extra_state_when_parent_collected()
	{
		WeakReference weakSampleEntity;
		WeakReference weakSampleValue;

		void Inner()
		{
			var objEntity = new SampleEntity { Name = "Parent" };
			weakSampleEntity = new WeakReference(objEntity);

			var objValue = new SampleExtendedValue { Extra = "Child" };
			weakSampleValue = new WeakReference(objValue);

			objEntity.Attached.Child = objValue;

			Assert.That(objEntity.Attached.Child.Extra, Is.EqualTo("Child"));

			objEntity = null;
		}
		Inner();

		AssertDead(weakSampleEntity);
		AssertDead(weakSampleValue);
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
}

public class SampleEntity
{
	public required string Name { get; init; }
}

public class SampleExtendedValue
{
	public required string Extra { get; init; }
}
