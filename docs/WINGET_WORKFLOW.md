# Winget Release Automation

The repository includes an automated workflow for submitting new releases to the Windows Package Manager (Winget).

## How It Works

When a new release is published on GitHub (not a draft or prerelease), the workflow automatically:

1. Detects the msixbundle file in the release assets
2. Extracts the version from the release tag
3. Creates or updates the Winget package manifest for `Starpine.Screenbox`
4. Submits a pull request to the [winget-pkgs repository](https://github.com/microsoft/winget-pkgs)

## Setup Requirements

### WINGET_TOKEN Secret

The workflow requires a GitHub token with permission to create pull requests in the winget-pkgs repository. To set this up:

1. Generate a classic Personal Access Token (PAT) with `public_repo` scope:
   - Go to GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
   - Click "Generate new token (classic)"
   - Give it a descriptive name like "Screenbox Winget Releaser"
   - Select the `public_repo` scope
   - Set an appropriate expiration date
   - Generate and copy the token

2. Add the token as a repository secret:
   - Go to the Screenbox repository settings
   - Navigate to Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `WINGET_TOKEN`
   - Value: Paste the PAT from step 1
   - Click "Add secret"

## Workflow Trigger

The workflow is triggered automatically when:
- A release is published (transitions from draft to published)
- The release is not marked as a prerelease

## Manual Trigger

If needed, the workflow can be manually retriggered by:
1. Going to the Actions tab in the repository
2. Selecting the "Winget Release" workflow
3. Using the "Re-run jobs" option on a previous run

## Package Information

- **Package ID**: `Starpine.Screenbox`
- **Installer Type**: msixbundle
- **Repository**: [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs)

## Troubleshooting

If the workflow fails:

1. Check that the release includes a `.msixbundle` file
2. Verify the `WINGET_TOKEN` secret is set and valid
3. Check the workflow run logs in the Actions tab
4. Ensure the release tag follows the version format (e.g., `v0.17.0`)

For more information about the action used, see: https://github.com/vedantmgoyal9/winget-releaser
