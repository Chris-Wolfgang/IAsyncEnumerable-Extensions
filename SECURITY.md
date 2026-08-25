# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability, please follow these steps:

1. **Do not** create a public issue on this repository.
2. In the top navigation of this repository, click the **Security** tab.
3. In the top right, click the **Report a vulnerability** button.
4. Fill out the provided form with:
   - A description of the vulnerability
   - Steps to reproduce the issue
   - Potential impact
   - Suggested fix (if you have one)

## Response Timeline

We will acknowledge your report within 48 hours and provide an estimated timeline for a fix.

## Thank You

Your help is greatly appreciated!
Responsible disclosure of security vulnerabilities helps protect our entire community.

## OSSF Scorecard

[`scorecard.yaml`](.github/workflows/scorecard.yaml) runs the
[OSSF Scorecard](https://github.com/ossf/scorecard) weekly and on every push
to `main`, scoring this repo's security posture (branch protection, pinned
dependencies, dangerous-workflow patterns, vulnerability response time,
etc.) against the project's checks. Results publish to the
[Scorecard viewer](https://securityscorecards.dev/viewer/?uri=github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions)
and the badge in `README.md`, and upload as SARIF to this repo's Security
tab alongside CodeQL alerts.

**Score floor: 7.5.** The initial baseline score is whatever the first
scheduled run reports — there was no prior run to snapshot before this
workflow existed. If a later run drops the score below 7.5, note it in
`CHANGELOG.md` under `### Security` and open a maintenance issue for the
regressed check; don't let it sit unaddressed.
