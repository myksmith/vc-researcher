# Commit Message

## Format
```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

## Types
- **feat**: A new feature
- **fix**: A bug fix
- **docs**: Documentation only changes
- **style**: Changes that do not affect the meaning of the code (white-space, formatting, missing semi-colons, etc)
- **refactor**: A code change that neither fixes a bug nor adds a feature
- **perf**: A code change that improves performance
- **test**: Adding missing tests or correcting existing tests
- **chore**: Changes to the build process or auxiliary tools and libraries

## Examples
- `feat(auth): add user login functionality`
- `fix: resolve null reference exception in order processing`
- `docs: update API documentation for payment endpoints`
- `refactor(database): optimize customer query performance`

## Guidelines
- Use the imperative mood in the subject line ("add" not "added")
- Keep the subject line under 50 characters
- Capitalize the subject line
- Do not end the subject line with a period
- Use the body to explain what and why vs. how