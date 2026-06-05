namespace vcrutils
{
    public static class ResearchHelper
    {
        public static string BuildVCPrompt(string investorDomain, string investorCriteria)
        {
            return $"Research the venture capital firm at {investorDomain} and evaluate whether it would be a good fit as an investor, based on the specific criteria provided below.\n\n" +
                   $"INVESTOR CRITERIA CONTEXT:\n{investorCriteria}\n\n" +
                   $"IMPORTANT: Format your response as proper Markdown. Start your response with exactly this format on the first line:\n" +
                   $"VC Name: [Full Name of the VC Firm]\n\n" +
                   $"Then provide a comprehensive markdown analysis covering:\n" +
                   $"1. How well they match our stage, check size, and sector focus\n" +
                   $"2. Their relevant portfolio companies and track record\n" +
                   $"3. Geographic alignment and investment thesis fit\n" +
                   $"4. Overall recommendation (Strong Fit / Good Fit / Weak Fit / No Fit)\n" +
                   $"5. Any specific partners or team members to target\n" +
                   $"6. Potential concerns or red flags\n\n" +
                   $"Use proper markdown formatting with headers, bullet points, bold text, etc.";
        }

        public static string BuildFamilyOfficePrompt(string investorDomain, string investorCriteria)
        {
            return $"Research the family office at {investorDomain} and evaluate whether it would be a good fit as a direct investor in an early-stage B2B software company, based on the specific criteria provided below.\n\n" +
                   $"INVESTOR CRITERIA CONTEXT:\n{investorCriteria}\n\n" +
                   $"IMPORTANT: Format your response as proper Markdown. Start your response with exactly this format on the first line:\n" +
                   $"Family Office Name: [Full Name of the Family Office or Family]\n\n" +
                   $"Then provide a comprehensive markdown analysis covering:\n" +
                   $"1. **Nature of investor**: Confirm whether this is a genuine single-family office or multi-family office. If it invests only via funds (LP only), only in real estate, or only in public markets, state that plainly — this likely makes it a poor fit for direct startup investment.\n" +
                   $"2. **Source of wealth and principal**: The founding family, the source of their wealth, and any operating-company ties to telecoms, utilities, banking, transportation, health, or industrials (these sectors are our buyer base — a strategic angle matters more than financial return).\n" +
                   $"3. **Direct startup investing evidence**: Whether they invest DIRECTLY in startups (not only as LP in VC funds). Provide named portfolio companies, deal dates, and check sizes where available — not just stated intent.\n" +
                   $"4. **Investment parameters**: Typical direct-deal check size, preferred stage, sector focus, and whether they have a track record in early-stage B2B software, devtools, or security.\n" +
                   $"5. **Geographic alignment**: Whether they invest in UK/Europe-based companies or cross-border, given our primary market.\n" +
                   $"6. **Decision-maker**: Who actually decides — the principal, a CIO, or a gatekeeper/family-office manager — and any named individual to target.\n" +
                   $"7. **Warm-introduction path**: The most plausible route to a warm introduction (family offices run on trusted intros, not cold inbound).\n" +
                   $"8. **Overall recommendation**: Strong Fit / Good Fit / Weak Fit / No Fit.\n" +
                   $"9. **Concerns / red flags**: Real-estate or public-markets only, no early-stage tech, opaque structure, or no verifiable direct startup activity.\n\n" +
                   $"Use proper markdown formatting with headers, bullet points, bold text, etc.";
        }

        public static string ExtractInvestorNameFromResponse(string response, InvestorType type)
        {
            string header = type == InvestorType.FamilyOffice ? "Family Office Name:" : "VC Name:";
            string fallback = type == InvestorType.FamilyOffice ? "Unknown Family Office" : "Unknown VC";

            var lines = response.Split('\n');
            if (lines.Length > 0)
            {
                string firstLine = lines[0].Trim();
                if (firstLine.StartsWith(header, StringComparison.OrdinalIgnoreCase))
                {
                    string name = firstLine.Substring(header.Length).Trim();
                    name = name.Replace("**", "").Replace("*", "").Trim();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }

            return fallback;
        }
    }
}
