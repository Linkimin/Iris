using Iris.Domain.Common;
using Iris.Domain.Memories;

namespace Iris.Domain.Tests.Memories;

public sealed class MemoryTests
{
    private static readonly DateTimeOffset _now = new(2025, 5, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidInputs_ReturnsActiveMemory()
    {
        var memory = Memory.Create(
            MemoryId.New(),
            MemoryContent.Create("Мой любимый язык — C#."),
            MemoryKind.Fact,
            MemoryImportance.Normal,
            MemorySource.UserExplicit,
            _now);

        Assert.Equal(MemoryStatus.Active, memory.Status);
        Assert.Equal(MemoryKind.Fact, memory.Kind);
        Assert.Equal(MemoryImportance.Normal, memory.Importance);
        Assert.Equal(MemorySource.UserExplicit, memory.Source);
        Assert.Equal(_now, memory.CreatedAt);
        Assert.Null(memory.UpdatedAt);
    }

    [Fact]
    public void Forget_OnActive_TransitionsToForgottenAndReturnsTrue()
    {
        Memory memory = CreateActiveMemory();

        var result = memory.Forget(_now.AddHours(1));

        Assert.True(result);
        Assert.Equal(MemoryStatus.Forgotten, memory.Status);
        Assert.Equal(_now.AddHours(1), memory.UpdatedAt);
    }

    [Fact]
    public void Forget_OnAlreadyForgotten_ReturnsFalseAndDoesNotChangeState()
    {
        Memory memory = CreateActiveMemory();
        memory.Forget(_now.AddHours(1));
        DateTimeOffset? updatedAtAfterFirstForget = memory.UpdatedAt;

        var result = memory.Forget(_now.AddHours(2));

        Assert.False(result);
        Assert.Equal(MemoryStatus.Forgotten, memory.Status);
        Assert.Equal(updatedAtAfterFirstForget, memory.UpdatedAt);
    }

    [Fact]
    public void UpdateContent_OnActive_ReplacesContentAndUpdatesTimestamp()
    {
        Memory memory = CreateActiveMemory();
        var newContent = MemoryContent.Create("Мой любимый цвет — синий.");

        memory.UpdateContent(newContent, _now.AddHours(1));

        Assert.Equal(newContent, memory.Content);
        Assert.Equal(_now.AddHours(1), memory.UpdatedAt);
    }

    [Fact]
    public void UpdateContent_OnForgotten_ThrowsDomainException()
    {
        Memory memory = CreateActiveMemory();
        memory.Forget(_now.AddHours(1));

        var newContent = MemoryContent.Create("Мой любимый цвет — синий.");

        DomainException exception = Assert.Throws<DomainException>(
            () => memory.UpdateContent(newContent, _now.AddHours(2)));

        Assert.Equal("memory.not_active", exception.Code);
    }

    private static Memory CreateActiveMemory()
    {
        return Memory.Create(
            MemoryId.New(),
            MemoryContent.Create("Мой любимый язык — C#."),
            MemoryKind.Fact,
            MemoryImportance.Normal,
            MemorySource.UserExplicit,
            _now);
    }
}
