using System;
using System.IO;
using Inceptum.AppServer.Configuration.Providers;
using NUnit.Framework;

namespace Inceptum.Configuration.Tests.Client
{
    [TestFixture]
    public class FileSystemConfigurationProviderTests
    {
        private readonly string m_ConfigFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            if (Directory.Exists(m_ConfigFolder))
                Directory.Delete(m_ConfigFolder, true);
            Directory.CreateDirectory(m_ConfigFolder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_ConfigFolder))
                Directory.Delete(m_ConfigFolder, true);
        }

        #endregion

        [Test]
        public void EndToEndTest()
        {
            Directory.CreateDirectory(Path.Combine(m_ConfigFolder, "testConfiguration"));
            string path = Path.Combine(m_ConfigFolder, "testConfiguration", "test.file.with.many.extensions");
            File.WriteAllText(path, "abc");
            var provider = new FileSystemConfigurationProvider(m_ConfigFolder);
            string content = provider.GetBundle("testConfiguration", "test", "file", "with", "many", "extensions");
            Assert.That(content, Is.EqualTo("abc"), "Conent was not loaded");
        }
        
        [Test]
        public void CtorCreasConfFolderIfItDoesNotExistTest()
        {
            string path = Path.Combine(m_ConfigFolder, "TEST");
            new FileSystemConfigurationProvider(path);
            Assert.That(Directory.Exists(path),Is.True);
        }
        
        
        [Test]
        public void StoreBundleTest()
        {
            var provider = new FileSystemConfigurationProvider(m_ConfigFolder);
            provider.StoreBundle("testConfiguration","test", new[] {"param1", "param2", "param3"}, "content");
            Assert.That(File.Exists(Path.Combine(m_ConfigFolder, "testConfiguration", "test.param1.param2.param3")), Is.True, "Bundle was not stored");
            Assert.That(File.ReadAllText(Path.Combine(m_ConfigFolder, "testConfiguration","test.param1.param2.param3")), Is.EqualTo("content"), "Bundle content was corrupted");
        }
        
        [Test]
        public void StoreBundleRewriteTest()
        {
            var filePath = Path.Combine(m_ConfigFolder,"testConfiguration", "test.param1.param2.param3");
            Directory.CreateDirectory(Path.Combine(m_ConfigFolder, "testConfiguration"));
            File.WriteAllText(filePath, "abc");
            var fi = new FileInfo(filePath);
            fi.Attributes = fi.Attributes|FileAttributes.ReadOnly;
            
            var provider = new FileSystemConfigurationProvider(m_ConfigFolder);
            provider.StoreBundle("testConfiguration","test", new[] {"param1", "param2", "param3"}, "content");
            Assert.That(File.Exists(filePath), Is.True, "Bundle was not stored");
            Assert.That(File.ReadAllText(filePath), Is.EqualTo("content"), "Bundle content was corrupted");
        }

        [Test]
        public void GetResourceContentTest()
        {
            string path = Path.Combine(m_ConfigFolder, "test.file");
            File.WriteAllText(path, "abc");
            var provider = new FileSystemConfigurationProvider(m_ConfigFolder);
            string content = provider.GetResourceContent(path);
            Assert.That(content, Is.EqualTo("abc"), "Conent was not loaded");
        }

        [Test]
        public void GetResourceNameExtraParamsTest()
        {
            var provider = new FileSystemConfigurationProvider(m_ConfigFolder);
            string resourceName = provider.GetResourceName("testConfiguration", "ibank.some.data", "with", "extra", "params");
            string directoryName = Path.GetDirectoryName(resourceName);
            string fileName = Path.GetFileName(resourceName);
            Assert.That(directoryName, Is.EqualTo(Path.Combine(m_ConfigFolder,"testConfiguration")),
                        "File containing bundle values is looked up in wrong dir");
            Assert.That(fileName, Is.EqualTo("ibank.some.data.with.extra.params"),
                        "Wrong file is used to take bundle values");
        }

        [Test]
        public void GetResourceNameNoExtraParamsTest()
        {
            var provider = new FileSystemConfigurationProvider(m_ConfigFolder);
            string resourceName = provider.GetResourceName("testConfiguration", "ibank.some.data");
            string directoryName = Path.GetDirectoryName(resourceName);
            string fileName = Path.GetFileName(resourceName);
            Assert.That(directoryName, Is.EqualTo(Path.Combine(m_ConfigFolder,"testConfiguration")),
                        "File containing bundle values is looked up in wrong dir");
            Assert.That(fileName, Is.EqualTo("ibank.some.data"), "Wrong file is used to take bundle values");
        }
    }
}