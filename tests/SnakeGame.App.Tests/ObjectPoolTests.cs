using FluentAssertions;
using SnakeGame.App.Rendering;
using Xunit;

namespace SnakeGame.App.Tests;

public class ObjectPoolTests
{
    [Fact]
    public void Rent_WhenPoolEmpty_ReturnsNewInstance()
    {
        // Arrange
        var pool = new ObjectPool<TestPooledObject>();

        // Act
        var item = pool.Rent();

        // Assert
        item.Should().NotBeNull();
        item.Value.Should().Be(0);
        pool.Count.Should().Be(0);
    }

    [Fact]
    public void Return_WhenCalled_AddsItemToPool()
    {
        // Arrange
        var pool = new ObjectPool<TestPooledObject>();
        var item = pool.Rent();
        item.Value = 42;

        // Act
        pool.Return(item);

        // Assert
        pool.Count.Should().Be(1);
    }

    [Fact]
    public void Rent_WhenPoolHasItems_ReturnsPooledItem()
    {
        // Arrange
        var pool = new ObjectPool<TestPooledObject>();
        var item1 = new TestPooledObject { Value = 42 };
        pool.Return(item1);

        // Act
        var item2 = pool.Rent();

        // Assert
        item2.Should().BeSameAs(item1);
        item2.Value.Should().Be(0); // Reset was called
    }

    [Fact]
    public void Return_WhenPoolFull_DiscardsItem()
    {
        // Arrange
        var pool = new ObjectPool<TestPooledObject>(maxSize: 2);
        pool.Return(new TestPooledObject());
        pool.Return(new TestPooledObject());

        // Act
        pool.Return(new TestPooledObject()); // Pool is full

        // Assert
        pool.Count.Should().Be(2);
    }

    [Fact]
    public void Return_CallsResetAction()
    {
        // Arrange
        var resetCalled = false;
        var pool = new ObjectPool<TestPooledObject>(resetAction: _ => resetCalled = true);
        var item = new TestPooledObject();

        // Act
        pool.Return(item);

        // Assert
        resetCalled.Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        // Arrange
        var pool = new ObjectPool<TestPooledObject>();
        pool.Return(new TestPooledObject());
        pool.Return(new TestPooledObject());

        // Act
        pool.Clear();

        // Assert
        pool.Count.Should().Be(0);
    }

    [Fact]
    public void MultipleRentReturn_ShouldReuseObjects()
    {
        // Arrange
        var pool = new ObjectPool<TestPooledObject>();

        // Act - First cycle
        var item1 = pool.Rent();
        pool.Return(item1);

        // Act - Second cycle
        var item2 = pool.Rent();
        pool.Return(item2);

        // Assert
        item2.Should().BeSameAs(item1);
    }
}

/// <summary>
/// Test pooled object
/// </summary>
sealed class TestPooledObject : IPoolable
{
    public int Value { get; set; }

    public void Reset()
    {
        Value = 0;
    }
}