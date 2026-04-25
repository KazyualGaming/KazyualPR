#!/usr/bin/env python3

"Post a specific changelog entry to Discord for a merged PR."

import os
import sys

import requests
import yaml

CHANGELOG_FILE = "Resources/Changelog/Sandwich.yml"
DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL")
CHANGELOG_ID = os.environ.get("CHANGELOG_ID")
PR_NUMBER = os.environ.get("PR_NUMBER", "?")
GITHUB_REPOSITORY = os.environ.get("GITHUB_REPOSITORY", "")

CHANGELOG_ROLE_ID = "1491506525192061150"

TYPES_TO_EMOJI = {"Fix": "🐛", "Add": "🆕", "Remove": "❌", "Tweak": "⚒️"}


def main():
    if not DISCORD_WEBHOOK_URL:
        print("No Discord webhook URL found, skipping")
        return

    if not CHANGELOG_ID:
        print("No changelog ID provided, skipping")
        return

    with open(CHANGELOG_FILE, "r") as f:
        data = yaml.safe_load(f)

    target_id = int(CHANGELOG_ID)
    entry = next((e for e in data.get("Entries", []) if e["id"] == target_id), None)
    if not entry:
        print(f"Changelog entry {target_id} not found, skipping")
        return

    author = entry.get("author", "Unknown")
    changes = entry.get("changes", [])
    pr_url = f"https://github.com/{GITHUB_REPOSITORY}/pull/{PR_NUMBER}"

    lines = [f"<@&{CHANGELOG_ROLE_ID}>\n", f"**{author}** updated:\n"]
    for change in changes:
        emoji = TYPES_TO_EMOJI.get(change.get("type", ""), "❓")
        lines.append(f"{emoji} - {change['message']}\n")
    lines.append(f"[PR #{PR_NUMBER}]({pr_url})\n")

    content = "".join(lines)
    response = requests.post(DISCORD_WEBHOOK_URL + "?wait=true", json={
        "content": content,
        "allowed_mentions": {"roles": [CHANGELOG_ROLE_ID]},
        "flags": 1 << 2,
    })
    response.raise_for_status()
    message_id = response.json()["id"]
    print(f"Posted changelog for PR #{PR_NUMBER} (entry #{target_id}) to Discord")

    crosspost = requests.post(f"{DISCORD_WEBHOOK_URL}/messages/{message_id}/crosspost")
    crosspost.raise_for_status()
    print(f"Published message {message_id} to announcement channel followers")


if __name__ == "__main__":
    main()
