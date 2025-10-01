using System;
using System.Threading.Tasks;
using Xunit;
using vcrutils;

namespace vcrutils.Tests
{
    public class AttioHelperTests
    {
        [Fact]
        public void GetAttioClient_ShouldThrowException_WhenApiKeyNotSet()
        {
            Environment.SetEnvironmentVariable("ATTIO_API_KEY", null);
            Assert.Throws<InvalidOperationException>(() => AttioHelper.GetAttioClient());
        }

        [Fact]
        public async Task FindAttioRecord_ShouldReturnNull_WhenApiKeyNotSet()
        {
            Environment.SetEnvironmentVariable("ATTIO_API_KEY", null);
            string? result = await AttioHelper.FindAttioRecord("dummyDomain");
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAttioCompanyRecord_ShouldReturnFalse_WhenApiKeyNotSet()
        {
            Environment.SetEnvironmentVariable("ATTIO_API_KEY", null);
            HttpClient client = new HttpClient(); // Dummy client for testing
            bool result = await AttioHelper.UpdateAttioCompanyRecord(client, "dummyRecordId", "dummyUrl");
            Assert.False(result);
        }

        [Fact]
        public async Task CreateAttioNote_ShouldReturnFalse_WhenApiKeyNotSet()
        {
            Environment.SetEnvironmentVariable("ATTIO_API_KEY", null);
            bool result = await AttioHelper.CreateAttioNote("dummyRecordId", "dummyTitle", "dummyContent", NoteFormat.Plaintext);
            Assert.False(result);
        }
    }
}
