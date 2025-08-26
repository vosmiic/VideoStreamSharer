using VideoStreamBackend.Helpers;

namespace UnitTests.Helpers;

[TestFixture]
public class StreamHelperTests {
    [TestCase("testing/this/f8a4182a-7eb9-11f0-a50f-18c04ddd08ba/input", true)]
    [TestCase("testingthisf8a4182a-7eb9-11f0-a50f-18c04ddd08bainput", true)]
    [TestCase("testingthisf8a4182a7eb911f0a50f1c04ddd08bainput", false)]
    public void FindUserIdTest(string input, bool shouldMatch) {
        var result = StreamHelper.FindUserId(input);
        Assert.That(result, shouldMatch ? Is.Not.Null : Is.Null);
        if (result != null) {
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
        }
    }
}