using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaiveMusicUpdater;
using System;
using System.IO;
using System.Linq;

namespace NaiveTests
{
    [TestClass]
    public class Configs
    {
        [TestMethod]
        [DeploymentItem(@"TestLibrary")]
        public void Library()
        {
            var library = new MusicLibrary(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestLibrary"));
            var song = library.GetAllSongs().Single();
            var metadata = song.GetMetadata(MetadataField.All);
            AssertMetadata(metadata, MetadataField.Title, "LITERAL");
        }

        private void AssertMetadata(Metadata meta, MetadataField field, string single_value)
        {

        }
    }
}
