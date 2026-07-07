using System.Linq;
using Core.Services;
using NUnit.Framework;

namespace Core.Tests;

[TestFixture]
public class SmsServiceTests
{
    [Test]
    public void SplitIntoBatches_ReturnsExpectedChunkSizes()
    {
        var numbers = Enumerable.Range(1, 105)
            .Select(i => $"+491700000{i:000}")
            .ToArray();

        var batches = SmsService.SplitIntoBatches(numbers, 25);

        Assert.That(batches.Count, Is.EqualTo(5));
        Assert.That(batches[0].Length, Is.EqualTo(25));
        Assert.That(batches[4].Length, Is.EqualTo(5));
    }
}
