using BC.Editor.Foundation.Pickers;
using NUnit.Framework;

namespace BC.Editor.Tests
{
    public sealed class EditorFoundationPickerTests
    {
        [Test]
        public void PickerButtonContentBuilderUsesRequiredStateLabels()
        {
            Assert.AreEqual(
                "Current",
                PickerButtonContentBuilder.Build(PickerSelectionState.Current, "Current", null, "Any", 1).text);

            Assert.That(
                PickerButtonContentBuilder.Build(PickerSelectionState.Incompatible, "Stored", null, "Local", 3).text,
                Does.StartWith("Incompatible:"));

            Assert.That(
                PickerButtonContentBuilder.Build(PickerSelectionState.Missing, null, "Lost", "Any", 0).text,
                Does.StartWith("Missing:"));

            Assert.AreEqual(
                "None",
                PickerButtonContentBuilder.Build(PickerSelectionState.None, null, null, "Any", 0).text);
        }

        [Test]
        public void PickerStateCacheReusesSameStateByKey()
        {
            PickerStateCache cache = new();

            Assert.AreSame(cache.GetOrCreate("key"), cache.GetOrCreate("key"));
            Assert.AreNotSame(cache.GetOrCreate("key"), cache.GetOrCreate("other"));
        }
    }
}
