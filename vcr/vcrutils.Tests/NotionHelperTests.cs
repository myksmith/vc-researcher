using System;
using System.Threading.Tasks;
using Xunit;
using vcrutils;

namespace vcrutils.Tests
{
    public class NotionHelperTests
    {
        [Fact]
        public void GetNotionClient_ShouldThrowException_WhenApiKeyNotSet()
        {
            Environment.SetEnvironmentVariable("NOTION_API_KEY", null);
            Assert.Throws<InvalidOperationException>(() => NotionHelper.GetNotionClient());
        }

        [Fact]
        public async Task AppendMarkdownToNotionPage_ShouldReturnFalse_WhenApiKeyNotSet()
        {
            Environment.SetEnvironmentVariable("MARK2NOTION_API_KEY", null);
            bool result = await NotionHelper.AppendMarkdownToNotionPage("dummyPageId", "dummyContent", "dummyToken");
            Assert.False(result);
        }

        [Fact]
        public async Task CreateNotionInvestorEntry_ShouldReturnNull_WhenApiKeyNotSet()
        {
            Environment.SetEnvironmentVariable("NOTION_API_KEY", null);
            string? result = await NotionHelper.CreateNotionInvestorEntry("dummyDomain", "dummyName", "dummyContent");
            Assert.Null(result);
        }

        [Fact]
        public async Task CheckNotionDomainExists_ShouldReturnFalse_WhenApiKeyNotSet()
        {
            Environment.SetEnvironmentVariable("NOTION_API_KEY", null);
            bool result = await NotionHelper.CheckNotionDomainExists("dummyDomain");
            Assert.False(result);
        }

        [Fact]
        public async Task FindExistingNotionPageId_ShouldReturnNull_WhenApiKeyNotSet()
        {
            Environment.SetEnvironmentVariable("NOTION_API_KEY", null);
            string? result = await NotionHelper.FindExistingNotionPageId("dummyDomain");
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteNotionPage_ShouldReturnFalse_WhenApiKeyNotSet()
        {
            Environment.SetEnvironmentVariable("NOTION_API_KEY", null);
            bool result = await NotionHelper.DeleteNotionPage("dummyPageId");
            Assert.False(result);
        }
    }
}
