using Xunit;
using vcrutils;

namespace vcrutils.Tests
{
    public class ResearchHelperTests
    {
        // ── ExtractInvestorNameFromResponse ───────────────────────────────────

        [Fact]
        public void ExtractName_VC_ReturnsNameFromHeader()
        {
            string response = "VC Name: Sequoia Capital\n\nSome analysis here.";
            string result = ResearchHelper.ExtractInvestorNameFromResponse(response, InvestorType.VC);
            Assert.Equal("Sequoia Capital", result);
        }

        [Fact]
        public void ExtractName_FamilyOffice_ReturnsNameFromHeader()
        {
            string response = "Family Office Name: Bessemer Trust\n\nSome analysis here.";
            string result = ResearchHelper.ExtractInvestorNameFromResponse(response, InvestorType.FamilyOffice);
            Assert.Equal("Bessemer Trust", result);
        }

        [Fact]
        public void ExtractName_VC_FallsBackWhenNoHeader()
        {
            string response = "Some analysis with no name header.";
            string result = ResearchHelper.ExtractInvestorNameFromResponse(response, InvestorType.VC);
            Assert.Equal("Unknown VC", result);
        }

        [Fact]
        public void ExtractName_FamilyOffice_FallsBackWhenNoHeader()
        {
            string response = "Some analysis with no name header.";
            string result = ResearchHelper.ExtractInvestorNameFromResponse(response, InvestorType.FamilyOffice);
            Assert.Equal("Unknown Family Office", result);
        }

        [Fact]
        public void ExtractName_VC_WrongHeaderType_FallsBack()
        {
            // FamilyOffice header should not match when type is VC
            string response = "Family Office Name: Bessemer Trust\n\nSome analysis.";
            string result = ResearchHelper.ExtractInvestorNameFromResponse(response, InvestorType.VC);
            Assert.Equal("Unknown VC", result);
        }

        [Fact]
        public void ExtractName_StripsBoldMarkdown()
        {
            string response = "VC Name: **Sequoia Capital**\n\nSome analysis.";
            string result = ResearchHelper.ExtractInvestorNameFromResponse(response, InvestorType.VC);
            Assert.Equal("Sequoia Capital", result);
        }

        [Fact]
        public void ExtractName_CaseInsensitiveHeader()
        {
            string response = "vc name: Accel Partners\n\nSome analysis.";
            string result = ResearchHelper.ExtractInvestorNameFromResponse(response, InvestorType.VC);
            Assert.Equal("Accel Partners", result);
        }

        [Fact]
        public void ExtractName_EmptyNameAfterHeader_FallsBack()
        {
            string response = "VC Name:   \n\nSome analysis.";
            string result = ResearchHelper.ExtractInvestorNameFromResponse(response, InvestorType.VC);
            Assert.Equal("Unknown VC", result);
        }

        [Fact]
        public void ExtractName_EmptyResponse_FallsBack()
        {
            string result = ResearchHelper.ExtractInvestorNameFromResponse("", InvestorType.VC);
            Assert.Equal("Unknown VC", result);
        }

        // ── BuildVCPrompt ─────────────────────────────────────────────────────

        [Fact]
        public void BuildVCPrompt_ContainsCorrectFirstLineHeader()
        {
            string prompt = ResearchHelper.BuildVCPrompt("sequoiacap.com", "criteria");
            Assert.Contains("VC Name: [Full Name of the VC Firm]", prompt);
        }

        [Fact]
        public void BuildVCPrompt_ContainsDomain()
        {
            string prompt = ResearchHelper.BuildVCPrompt("sequoiacap.com", "criteria");
            Assert.Contains("sequoiacap.com", prompt);
        }

        [Fact]
        public void BuildVCPrompt_ContainsRecommendationOptions()
        {
            string prompt = ResearchHelper.BuildVCPrompt("sequoiacap.com", "criteria");
            Assert.Contains("Strong Fit / Good Fit / Weak Fit / No Fit", prompt);
        }

        [Fact]
        public void BuildVCPrompt_ContainsCriteriaContext()
        {
            string prompt = ResearchHelper.BuildVCPrompt("sequoiacap.com", "my-specific-criteria");
            Assert.Contains("my-specific-criteria", prompt);
        }

        [Fact]
        public void BuildVCPrompt_DoesNotContainFamilyOfficeHeader()
        {
            string prompt = ResearchHelper.BuildVCPrompt("sequoiacap.com", "criteria");
            Assert.DoesNotContain("Family Office Name:", prompt);
        }

        // ── BuildFamilyOfficePrompt ───────────────────────────────────────────

        [Fact]
        public void BuildFamilyOfficePrompt_ContainsCorrectFirstLineHeader()
        {
            string prompt = ResearchHelper.BuildFamilyOfficePrompt("example-fo.com", "criteria");
            Assert.Contains("Family Office Name: [Full Name of the Family Office or Family]", prompt);
        }

        [Fact]
        public void BuildFamilyOfficePrompt_ContainsDomain()
        {
            string prompt = ResearchHelper.BuildFamilyOfficePrompt("example-fo.com", "criteria");
            Assert.Contains("example-fo.com", prompt);
        }

        [Fact]
        public void BuildFamilyOfficePrompt_ContainsDirectVsLPDistinction()
        {
            string prompt = ResearchHelper.BuildFamilyOfficePrompt("example-fo.com", "criteria");
            Assert.Contains("LP", prompt);
            Assert.Contains("DIRECTLY", prompt);
        }

        [Fact]
        public void BuildFamilyOfficePrompt_ContainsSourceOfWealthPoint()
        {
            string prompt = ResearchHelper.BuildFamilyOfficePrompt("example-fo.com", "criteria");
            Assert.Contains("Source of wealth", prompt);
        }

        [Fact]
        public void BuildFamilyOfficePrompt_ContainsStrategicSectors()
        {
            string prompt = ResearchHelper.BuildFamilyOfficePrompt("example-fo.com", "criteria");
            Assert.Contains("telecoms", prompt);
            Assert.Contains("industrials", prompt);
        }

        [Fact]
        public void BuildFamilyOfficePrompt_ContainsWarmIntroPoint()
        {
            string prompt = ResearchHelper.BuildFamilyOfficePrompt("example-fo.com", "criteria");
            Assert.Contains("warm introduction", prompt);
        }

        [Fact]
        public void BuildFamilyOfficePrompt_ContainsRecommendationOptions()
        {
            string prompt = ResearchHelper.BuildFamilyOfficePrompt("example-fo.com", "criteria");
            Assert.Contains("Strong Fit / Good Fit / Weak Fit / No Fit", prompt);
        }

        [Fact]
        public void BuildFamilyOfficePrompt_ContainsRedFlagsPoint()
        {
            string prompt = ResearchHelper.BuildFamilyOfficePrompt("example-fo.com", "criteria");
            Assert.Contains("red flags", prompt);
            Assert.Contains("real estate", prompt.ToLower());
        }

        [Fact]
        public void BuildFamilyOfficePrompt_DoesNotContainVCHeader()
        {
            string prompt = ResearchHelper.BuildFamilyOfficePrompt("example-fo.com", "criteria");
            Assert.DoesNotContain("VC Name:", prompt);
        }
    }
}
