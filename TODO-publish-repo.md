# TODO: Before Publishing Repo

## Code Changes

- [x] Rename `Neo_Investor_Search_Criteria.md` → `investor_criteria.md` and `Neo_FamilyOffice_Search_Criteria.md` → `family_office_criteria.md`; strip Neo-specific content and replace with generic template/placeholder; update hardcoded filenames in `Program.cs:277-278`
- [x] Move hardcoded `NOTION_INVESTOR_RESEARCH_DATABASE_ID` in `NotionHelper.cs:14` to an env var (`NOTION_DATABASE_ID`)
- [x] Remove Neo-specific buyer sectors ("telecoms, utilities, banking, transportation, health, industrials") hardcoded in `ResearchHelper.cs:30-33` — this context should come from the criteria file, not the prompt template
- [x] Remove "sagittal Notion workspace" reference from error message in `TestCommands.cs:65`
- [x] Remove hardcoded Attio list names "Preseed VCs from Notion" and "Startup Fundraising" from `TestCommands.cs:162-235` — these are Neo-specific and meaningless to other users

## Documentation

- [x] Document required Notion database schema (needs a `Domain` URL field and `Investor Name` title field)
- [x] Document required Attio custom field (`notion_research_url` on company records)
- [x] Add setup walkthrough to README so a new user can configure their own Notion/Attio environment
