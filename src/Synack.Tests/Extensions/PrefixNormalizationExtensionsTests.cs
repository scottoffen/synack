using Synack.Extensions;

namespace Synack.Tests.Extensions;

public class PrefixNormalizationExtensionsTests
{
    [Theory]
    // basic roots and simple paths
    [InlineData("/", "/")]
    [InlineData("a", "/a/")]
    [InlineData("a/", "/a/")]
    [InlineData("/a", "/a/")]
    [InlineData("a/b", "/a/b/")]
    [InlineData("/a/b", "/a/b/")]
    // collapsing slashes
    [InlineData("//", "/")]
    [InlineData("//a///b", "/a/b/")]
    [InlineData("////a////", "/a/")]
    // backslashes and mixing slashes
    [InlineData(@"\", "/")]
    [InlineData(@"\a\b", "/a/b/")]
    [InlineData(@"a\b/c\", "/a/b/c/")]
    // dot segments
    [InlineData(".", "/")]
    [InlineData("..", "/")]
    [InlineData("/.", "/")]
    [InlineData("/..", "/")]
    [InlineData("/././", "/")]
    [InlineData("/a/./b/./", "/a/b/")]
    [InlineData("/a/..", "/")]
    [InlineData("/a/b/..", "/a/")]
    [InlineData("/a/../../b", "/b/")]
    [InlineData("/../b", "/b/")]
    [InlineData("a/../b", "/b/")]
    // literal dots inside a segment are kept
    [InlineData("/.../", "/.../")]
    [InlineData("/a..b/./c", "/a..b/c/")]
    // whitespace trimming at ends only
    [InlineData("  a  ", "/a/")]
    [InlineData("  /a/b  ", "/a/b/")]
    // specifically hit the in-loop '..' case to exercise `else { d = slashPos; }`
    [InlineData("/a/b/../c", "/a/c/")]
    public void NormalizePrefix_ReturnsExpected_WhenValidPath(string input, string expected)
    {
        var result = input.NormalizePrefix();
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    public void NormalizePrefix_ThrowsArgumentNullException_WhenNull(string? input)
    {
        Should.Throw<ArgumentNullException>(() => PrefixNormalizationExtensions.NormalizePrefix(input!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://host/a")]
    [InlineData("https://host/a")]
    [InlineData("/a?x=1")]
    [InlineData("/a#frag")]
    [InlineData("C:\\foo\\bar")]
    [InlineData("D:/stuff")]
    public void NormalizePrefix_ThrowsArgumentException_WhenInvalidPath(string input)
    {
        Should.Throw<ArgumentException>(() => input.NormalizePrefix());
    }

    // Forces segStack to spill into segList and then performs a '..' inside the loop to pop from the list.
    // Covers: list non-empty path -> `int idx = list[^1]; list.RemoveAt(...); return idx;`
    [Fact]
    public void NormalizePrefix_PopsFromSpilledList_WhenManySegmentsAndDotDotInsideLoop()
    {
        // 140 segments ensures spillover beyond the stackalloc capacity (<=128)
        var segments = Enumerable.Repeat("a", 140).ToArray();
        var input = string.Join("/", segments) + "/../z";
        var expected = "/" + string.Join("/", segments.Take(139)) + "/z/";

        var result = input.NormalizePrefix();
        result.ShouldBe(expected);
    }

    // After spilling, pop more times than there are spilled items so segList becomes empty,
    // then pop again to hit: `if (list.Count == 0) return -1;`
    [Fact]
    public void NormalizePrefix_ReturnsRoot_WhenPoppingBeyondSpilledListEmptiesIt()
    {
        // Create >128 segments to trigger spill, then far more '..' than segments.
        var segCount = 140;
        var input = string.Join("/", Enumerable.Repeat("a", segCount)) + "/" +
                    string.Join("/", Enumerable.Repeat("..", segCount + 10));
        var expected = "/";

        var result = input.NormalizePrefix();
        result.ShouldBe(expected);
    }
}
