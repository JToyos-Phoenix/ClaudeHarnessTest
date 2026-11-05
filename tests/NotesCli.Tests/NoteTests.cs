using System.Runtime.CompilerServices;

namespace NotesCli.Tests;

/// <summary>Tests for src/NotesCli/Note.cs.</summary>
public sealed class NoteTests
{
    [Fact]
    public void New_AssignsIdOne_WhenNoExistingNotes()
    {
        var note = Note.New("hola", "mundo", []);

        Assert.Equal(1, note.Id);
        Assert.Equal("hola", note.Title);
        Assert.Equal("mundo", note.Body);
        Assert.False(string.IsNullOrEmpty(note.CreatedAt));
    }

    [Fact]
    public void New_IncrementsIdFromHighestExisting()
    {
        List<Note> existing = [new(1, "", "", ""), new(2, "", "", ""), new(5, "", "", "")];

        var note = Note.New("x", "y", existing);

        Assert.Equal(6, note.Id);
    }

    [Fact]
    public void New_SetsCreatedAtAsIso8601Utc()
    {
        var note = Note.New("a", "b", []);

        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\+00:00$", note.CreatedAt);
    }

    [Fact]
    public void Note_HasNoPublicMutableProperties()
    {
        var mutable = typeof(Note).GetProperties()
            .Where(p => p.SetMethod is { IsPublic: true } setter
                        && !setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit)))
            .Select(p => p.Name);

        Assert.Empty(mutable);
    }

    [Fact]
    public void With_CreatesNewInstance_AndLeavesOriginalUnchanged()
    {
        var original = Note.New("a", "b", []);

        var edited = original with { Title = "other" };

        Assert.Equal("a", original.Title);
        Assert.Equal("other", edited.Title);
        Assert.Equal(original.Id, edited.Id);
    }
}
